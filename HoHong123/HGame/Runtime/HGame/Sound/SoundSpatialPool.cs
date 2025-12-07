using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Audio;
using Util.Pooling;

namespace Util.Sound {
    public class SoundSpatialPool : MonoBehaviour {
        [Header("Pool Setup")]
        [SerializeField]
        int poolSize = 32;
        [SerializeField]
        Transform poolParent;

        [Header("3D Audio Settings")]
        [SerializeField]
        bool defaultLoop = false;
        [SerializeField]
        bool defaultPlayOnAwake = false;
        [SerializeField, Range(0f, 1f)]
        float defaultSpatialBlend = 1f; // 1 == full 3D
        [SerializeField]
        AudioRolloffMode defaultRolloffMode = AudioRolloffMode.Custom;
        [SerializeField]
        AnimationCurve defaultCustomRolloffCurve = AnimationCurve.Linear(0f, 1f, 10f, 0f); // 예시: 0m에서 볼륨1, 10m에서 1
        [SerializeField] 
        float defaultMinDistance = 0f;
        [SerializeField] 
        float defaultMaxDistance = 15f;

        [Header("Mixer Routing")]
        [SerializeField]
        AudioMixerGroup sfxMixerGroup;

        CancellationToken destroyToken;
        ComponentPool<AudioSource> audioPool;


        private void Awake() {
            destroyToken = this.GetCancellationTokenOnDestroy();
            audioPool = new(
                    null,
                    initialSize: poolSize,
                    poolParent,
                    onCreate: (audio) => {
                        audio.loop = defaultLoop;
                        audio.playOnAwake = defaultPlayOnAwake;
                        audio.spatialBlend = defaultSpatialBlend;
                        audio.rolloffMode = defaultRolloffMode;
                        audio.minDistance = defaultMinDistance;
                        audio.maxDistance = defaultMaxDistance;
                        audio.gameObject.SetActive(false);
                        if (defaultRolloffMode == AudioRolloffMode.Custom && defaultCustomRolloffCurve != null)
                            audio.SetCustomCurve(AudioSourceCurveType.CustomRolloff, defaultCustomRolloffCurve);
                        if (sfxMixerGroup)
                            audio.outputAudioMixerGroup = sfxMixerGroup;
                    },
                    onGet: (audio) =>
                    {
                        audio.gameObject.SetActive(true);
                        audio.volume = 1f;
                        audio.pitch = 1f;
                        audio.loop = false;
                        audio.clip = null;
                    },
                    onReturn: (audio) => {
                        audio.Stop();
                        audio.clip = null;
                        audio.transform.SetParent(poolParent ? poolParent : transform, false);
                        audio.gameObject.SetActive(false);
                    },
                    onDispose: (audio) => {
#if UNITY_EDITOR
                        if (audio) DestroyImmediate(audio.gameObject);
#else
                        if (audio) Destroy(audio.gameObject);
#endif
                    }
                );
        }


        /// <summary> 월드 좌표에서 원샷 재생(끝나면 자동 반납). </summary>
        public void PlayAt(AudioClip clip, Vector3 worldPos, float volume = 1f, float pitch = 1f) {
            if (!clip) return;
            var audio = audioPool.Get();
            audio.transform.SetParent(null);
            audio.transform.position = worldPos;
            _PlayAndReturnAsync(audio, clip, volume, pitch, destroyToken).Forget();
        }

        /// <summary> 부모 Transform 기준에서 원샷 재생(끝나면 자동 반납). </summary>
        public void PlayAt(AudioClip clip, Transform newParent, float volume = 1f, float pitch = 1f, bool keepWorldPosition = false) {
            if (!clip) return;
            var audio = audioPool.Get();
            audio.transform.SetParent(newParent, worldPositionStays: keepWorldPosition);
            if (!keepWorldPosition) audio.transform.localPosition = Vector3.zero;
            _PlayAndReturnAsync(audio, clip, volume, pitch, destroyToken).Forget();
        }

        public void StopAll() {
            var snapshot = new System.Collections.Generic.List<AudioSource>(audioPool.Activates);
            foreach (var audio in snapshot)
                audioPool.Return(audio);
        }


        private async UniTaskVoid _PlayAndReturnAsync(
            AudioSource audio, AudioClip clip, 
            float volume, float pitch, 
            CancellationToken token) {
            audio.clip = clip;
            audio.volume = volume;
            audio.pitch = pitch;
            audio.Play();

            try {
                // 종료 감시 : isPlaying 기준, 강제 Stop/씬 전환에도 안전
                await UniTask.WaitUntil(() => !audio || !audio.isPlaying, PlayerLoopTiming.Update, token);
            }
            catch {
                // 파괴/취소 시 무시
            }
            finally {
                if (audio) audioPool.Return(audio);
            }
        }
    }
}
