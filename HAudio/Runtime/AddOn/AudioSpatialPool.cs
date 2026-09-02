#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * 3D 원샷 SFX 를 AudioSource 풀로 재생하는 컴포넌트. 재생이 끝나면 자동 반납한다.
 *
 * 사용 예 ::
 *   spatialPool.PlayAt(clip, worldPos);
 *   spatialPool.PlayAt(clip, targetTransform);
 *
 * 특징 ::
 * ComponentPool<AudioSource> 기반. 3D 설정(spatialBlend / rolloff / 거리)은 onCreate 에서 1회 적용.
 * 종료 감시는 isPlaying 기준이라 외부 Stop 이나 씬 전환에도 반납이 성립한다.
 *
 * 소유권 경계 ::
 * 풀 소스는 항상 poolParent(미지정 시 자기 Transform) 아래에 머문다. 외부 Transform 에
 * SetParent 하지 않고 위치만 따라간다. 외부 계층에 넣으면 그 부모가 파괴될 때 자식인 풀
 * 소스까지 함께 파괴되어 풀이 재고를 잃기 때문이다.
 *
 * 주의사항 ::
 * - Transform 오버로드는 타깃이 파괴되면 추적만 멈추고 마지막 위치에서 클립을 끝까지 재생한다.
 * - onGet 이 loop 를 false 로 고정하므로 defaultLoop 는 현재 동작하지 않는 설정이다.
 * - StopAll 이 반납한 소스의 뒤늦은 재반납은 BasePool 의 장부 검사가 거른다.
 * =========================================================
 */
#endif

using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Audio;
using HUtil.Pooling;
using HDiagnosis.Logger;
using HInspector;

namespace HAudio.AddOn {
    public class AudioSpatialPool : MonoBehaviour {
        [HTitle("Pool Setup")]
        [SerializeField]
        int poolSize = 0;
        [SerializeField]
        Transform poolParent;

        [HTitle("3D Audio Settings")]
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

        [HTitle("Mixer Routing")]
        [SerializeField]
        AudioMixerGroup sfxMixerGroup;

        CancellationToken destroyToken;
        ComponentPool<AudioSource> audioPool;


        private void Awake() {
            destroyToken = this.GetCancellationTokenOnDestroy();
            // 풀 소스가 머무는 유일한 계층. 이 밖으로 내보내지 않는다.
            Transform sourceRoot = poolParent ? poolParent : transform;
            audioPool = new(
                    null,
                    initialSize: poolSize,
                    sourceRoot,
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
            audio.transform.position = worldPos;
            _Play(audio, clip, volume, pitch);
            _ReturnWhenFinishedAsync(audio, destroyToken).Forget();
        }

        /// <summary> 타깃 Transform 을 따라가며 원샷 재생(끝나면 자동 반납). </summary>
        public void PlayAt(AudioClip clip, Transform target, float volume = 1f, float pitch = 1f, bool keepWorldPosition = false) {
            if (!clip) return;
            if (!target) {
                HLogger.Warning("[AudioSpatialPool] PlayAt target is null or destroyed. Pass a live Transform or use the Vector3 overload.");
                return;
            }

            var audio = audioPool.Get();
            // 부모를 바꾸지 않고 타깃 로컬 기준 오프셋만 잡아 매 프레임 위치를 맞춘다.
            Vector3 localOffset = keepWorldPosition
                ? target.InverseTransformPoint(audio.transform.position)
                : Vector3.zero;
            audio.transform.position = target.TransformPoint(localOffset);
            _Play(audio, clip, volume, pitch);
            _FollowUntilFinishedAsync(audio, target, localOffset, destroyToken).Forget();
        }

        public void StopAll() {
            var snapshot = new System.Collections.Generic.List<AudioSource>(audioPool.Activates);
            foreach (var audio in snapshot)
                _Release(audio);
        }


        private void _Play(AudioSource audio, AudioClip clip, float volume, float pitch) {
            audio.clip = clip;
            audio.volume = volume;
            audio.pitch = pitch;
            audio.Play();
        }

        /// <summary> 살아있으면 풀에 반납하고, 외부에서 파괴됐으면 장부에서만 제거한다. </summary>
        private void _Release(AudioSource audio) {
            if (audio) audioPool.Return(audio);
            else audioPool.Discard(audio);
        }

        private async UniTaskVoid _ReturnWhenFinishedAsync(AudioSource audio, CancellationToken token) {
            try {
                // 종료 감시 : isPlaying 기준, 강제 Stop/씬 전환에도 안전
                await UniTask.WaitUntil(() => !audio || !audio.isPlaying, PlayerLoopTiming.Update, token);
            }
            catch (OperationCanceledException) {
                // 풀 파괴로 취소. 장부 정리는 finally 가 맡는다.
            }
            finally {
                _Release(audio);
            }
        }

        private async UniTaskVoid _FollowUntilFinishedAsync(
            AudioSource audio, Transform target,
            Vector3 localOffset, CancellationToken token) {
            try {
                while (audio && audio.isPlaying) {
                    // 타깃이 파괴되면 추적만 멈추고 마지막 위치에서 클립을 끝까지 재생한다.
                    if (target) audio.transform.position = target.TransformPoint(localOffset);
                    await UniTask.Yield(PlayerLoopTiming.Update, token);
                }
            }
            catch (OperationCanceledException) {
                // 풀 파괴로 취소. 장부 정리는 finally 가 맡는다.
            }
            finally {
                _Release(audio);
            }
        }
    }
}

#if UNITY_EDITOR
/* =========================================================
 * Dev Log
 * =========================================================
 *
 * =========================================================
 * 2026-09-02 (수정) :: 외부 Transform SetParent 제거 - 부모 파괴 시 풀 소스 소실
 * =========================================================
 * 변경 ::
 * 1. `PlayAt(AudioClip, Transform, ...)` 이 `SetParent(target)` 을 하지 않는다. 타깃 로컬
 *    기준 오프셋을 1회 계산해 두고 `_FollowUntilFinishedAsync` 가 매 Update 마다 월드 위치를
 *    맞춘다. `keepWorldPosition` 은 그 오프셋의 초기값만 정한다.
 * 2. `PlayAt(AudioClip, Vector3, ...)` 의 `SetParent(null)` 제거. 위치만 지정한다.
 * 3. `Awake` 에서 `sourceRoot = poolParent ? poolParent : transform` 을 풀 생성자에 넘긴다.
 *    소스의 부모가 생성 시점에 확정되므로 `onReturn` 의 `SetParent` 를 제거했다.
 * 4. 타깃이 null 이면 경고 후 반환. 이전에는 월드 원점에서 소리가 났다.
 * 5. `_PlayAndReturnAsync` 를 `_Play` / `_Release` / `_ReturnWhenFinishedAsync` /
 *    `_FollowUntilFinishedAsync` 로 분리. 포괄 `catch` 를 `OperationCanceledException` 으로 좁혔다.
 *
 * 이유 ::
 * Unity 는 부모 GameObject 를 파괴할 때 자식을 함께 파괴한다. 풀이 소유한 AudioSource 를
 * 외부 Transform 의 자식으로 넣으면, 그 외부 오브젝트(적, 투사체 등)가 죽는 순간 풀 소스도
 * 같이 죽는다. `AudioManager.Play3D(uid, Transform)` 이 정확히 이 경로다.
 * 죽은 소스는 `finally` 의 `if (audio)` 에 걸려 반납을 건너뛰므로 `activatedPool` 에 영원히
 * 남고, 풀은 재고가 줄어든 사실조차 모른 채 매번 새 GameObject 를 만든다. 풀링의 목적이
 * 무너진다. `SetParent(null)` 쪽도 소스를 씬 루트로 내보내 같은 위험을 만든다.
 * 소유권을 넘기지 않고 위치만 따라가면 이 경로가 통째로 사라진다.
 *
 * 결과 ::
 * 풀 소스는 생성부터 폐기까지 sourceRoot 아래에만 있다. 외부 오브젝트가 재생 도중 파괴돼도
 * 소스는 살아남아 마지막 위치에서 클립을 끝내고 정상 반납된다. 그래도 파괴되는 경로
 * (풀 자체 파괴 등) 는 `_Release` 가 `BasePool.Discard` 로 장부를 정리한다.
 * `onReturn` 이 파괴 중인 MonoBehaviour 의 `transform` 을 만지던 문제도 함께 없어졌다.
 *
 * 주의 ::
 * Transform 오버로드는 재생 중인 소스마다 매 Update 마다 위치를 1회 갱신한다. 부모 종속
 * 갱신이 엔진 쪽에서 처리되던 것을 스크립트로 옮긴 비용이다. 동시 재생 수가 수백 단위로
 * 늘면 측정이 필요하다.
 * `defaultLoop` 는 여전히 `onGet` 의 `loop = false` 에 덮여 동작하지 않는다. 이번 범위 밖이라
 * 남겨두었고 헤더 주의사항에 명시했다.
 *
 * =========================================================
 */
#endif
