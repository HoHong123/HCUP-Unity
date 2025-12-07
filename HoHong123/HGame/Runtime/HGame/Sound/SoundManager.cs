using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Audio;
using Sirenix.OdinInspector;
using Util.Data;
using Util.Logger;
using Cysharp.Threading.Tasks.Linq;
using Util.Diagnosis;

namespace Util.Sound {
    public class SoundManager : SingletonBehaviour<SoundManager> {
        [Serializable]
        public class SoundItem {
            public int Dependency;
            public AudioClip Clip;

            public SoundItem(int _dependency, AudioClip _clip) {
                Dependency = _dependency;
                Clip = _clip;
            }
        }

        [Title("Data Load")]
        [SerializeField]
        DataLoadType load = DataLoadType.Resources;

        [Title("Resource Path")]
        [SerializeField]
        string path = "Sounds/";
        [SerializeField]
        string sfxPath = "SFX/";
        [SerializeField]
        string bgmPath = "BGM/";

        [Title("Audio Mixer")]
        [SerializeField]
        AudioMixer audioMix;

        [Title("Audio Sources")]
        [SerializeField]
        AudioSource sfxAudio;
        [SerializeField]
        AudioSource bgmAudio;
        [SerializeField]
        AudioSource bgmAltAudio;
        [SerializeField]
        AudioSource uiAudio;
        [SerializeField]
        SoundSpatialPool spatialPool;

        [Title("Sound Data Allocation")]
        [SerializeField]
        [DictionaryDrawerSettings(KeyLabel = "Audio Code", ValueLabel = "Audio Clip")]
        readonly Dictionary<int, SoundItem> clipTable = new();
        readonly Dictionary<int, UniTask<AudioClip>> loading = new();


        #region ======== Registration ========
        public void PrewarmIds<TEnum>(TEnum id) where TEnum : Enum => PrewarmIds<TEnum>(Convert.ToInt32(id));
        public void PrewarmIds<TEnum>(int id) where TEnum : Enum => _LoadClip<TEnum>(id).Forget();

        public void PrewarmIds<TEnum>(IEnumerable<SFXView<TEnum>> ids) where TEnum : Enum {
            foreach (var id in ids)
                _LoadClip<TEnum>(id.Id).Forget();
        }

        public void PrewarmIds<TEnum>(IEnumerable<int> ids) where TEnum : Enum {
            foreach (var id in ids)
                _LoadClip<TEnum>(id).Forget();
        }

        public void PrewarmBGM<TEnum>(IEnumerable<int> ids) where TEnum : Enum {
            foreach (var id in ids)
                _LoadClip<TEnum>(id, SoundType.BGM).Forget();
        }

        public void ReleaseIds<TEnum>(IEnumerable<SFXView<TEnum>> ids) where TEnum : Enum {
            foreach (var id in ids) {
                int intID = id.Id;
                if (!clipTable.ContainsKey(intID) || --clipTable[intID].Dependency > 0)
                    continue;
                clipTable.Remove(intID);
            }
        }

        public void ReleaseIds(IEnumerable<int> ids) {
            foreach (var id in ids) {
                if (!clipTable.ContainsKey(id) || --clipTable[id].Dependency > 0)
                    continue;
                clipTable.Remove(id);
            }
        }
        #endregion

        #region ======== Play ========
        public void PlayClick() => PlayUI((int)SoundSFX.Click); // 가장 많이 사용되는 특수 케이스 전용 함수
        public void PlayUI(SoundSFX id) => PlayUI((int)id);
        public void Play<TEnum>(TEnum id) where TEnum : Enum => Play(Convert.ToInt32(id));
        public void Play3D<TEnum>(TEnum id, Transform parent) where TEnum : Enum => Play3D(Convert.ToInt32(id), parent);
        public void Play3D<TEnum>(TEnum id, Vector3 worldPos) where TEnum : Enum => Play3D(Convert.ToInt32(id), worldPos);
        public void PlayBGM(SoundBGM id, bool ignorSameClip = true) => PlayBGM((int)id, ignorSameClip);
        public UniTask CrossFadeBGM(SoundBGM id, bool ignorSameClip = true, float duration = 2f) => CrossFadeBGM((int)id, ignorSameClip, duration);

        public void PlayUI(int id) {
            if (!_CheckClipTable(id, out SoundItem item)) return;
            uiAudio.PlayOneShot(item.Clip);
        }

        public void Play(int id) {
            if (!_CheckClipTable(id, out SoundItem item)) return;
            sfxAudio.PlayOneShot(item.Clip);
        }

        public void Play3D(int id, Transform parent) {
            if (!_CheckClipTable(id, out SoundItem item)) return;
            spatialPool.PlayAt(item.Clip, parent, sfxAudio.volume);
        }

        public void Play3D(int id, Vector3 worldPos) {
            if (!_CheckClipTable(id, out SoundItem item)) return;
            spatialPool.PlayAt(item.Clip, worldPos, sfxAudio.volume);
        }

        public void PlayBGM(int id, bool ignorSameClip = true) {
            if (!_CheckClipTable(id, out SoundItem item)) return;
            if (ignorSameClip && bgmAudio.clip == item.Clip) return;
            bgmAudio.clip = item.Clip;
            bgmAudio.Play();
        }

        public async UniTask CrossFadeBGM(int id, bool ignorSameClip = true, float duration = 2f) {
            if (!_CheckClipTable(id, out SoundItem item)) return;
            if (ignorSameClip && bgmAudio.clip == item.Clip) return;

            bgmAltAudio.clip = item.Clip;
            bgmAltAudio.volume = 0f;
            bgmAltAudio.Play();

            float time = 0f;
            float volume = bgmAudio.volume;
            while (time < duration) {
                time += Time.unscaledDeltaTime;
                float fade = time / duration;
                bgmAudio.volume = Mathf.Lerp(volume, 0f, fade);
                bgmAltAudio.volume = Mathf.Lerp(0f, volume, fade);
                await UniTask.Yield();
            }

            (bgmAudio, bgmAltAudio) = (bgmAltAudio, bgmAudio);

            bgmAltAudio.Stop();
            bgmAltAudio.clip = null;
            bgmAltAudio.volume = 0f;
        }

        public async void StopBGM(float fadeOut = 0f) {
            if (fadeOut <= 0f) {
                bgmAudio.Stop();
                return;
            }

            float time = 0f;
            float volume = bgmAudio.volume;
            while (time < fadeOut) {
                time += Time.unscaledDeltaTime;
                float fade = Mathf.Clamp01(time / fadeOut);
                bgmAudio.volume = Mathf.Lerp(volume, 0f, fade);
                await UniTask.Yield();
            }

            bgmAudio.Stop();
            bgmAudio.volume = volume;
        }


        private bool _CheckClipTable(int id, out SoundItem item) {
            if (!clipTable.TryGetValue(id, out var fromTable) || fromTable.Clip == null) {
                HDebug.StackTraceError($"[SoundManager] Fail to load clip data({id}) in clip table.", 10);
                item = null;
                return false;
            }
            item = clipTable[id];
            return true;
        }
        #endregion


        private async UniTask<AudioClip> _LoadClip<TEnum>(int id, SoundType soundType = SoundType.SFX) where TEnum : Enum {
            if (clipTable.TryGetValue(id, out var item) && item.Clip) return item.Clip;
            if (loading.TryGetValue(id, out var loadingTask)) return await loadingTask;

            string resourcePath = _BuildPath<TEnum>(id, soundType);
            var task = _LoadClipInternal(resourcePath);
            loading[id] = task;
            var clip = await task;
            loading.Remove(id);
            if (!clip) return null;

            clipTable[id] = new SoundItem(1, clip);
            return clip;
        }

        private string _BuildPath<TEnum>(int enumNum, SoundType basicType) where TEnum : Enum {
            // Ex) "Sounds/SFX/MonsterSFX/630003_MonstHit"
            // Ex) "Sounds/BGM/680000_ThousandArrow"
            string typeName = basicType == SoundType.BGM ? string.Empty : typeof(TEnum).Name + "/";
            string prefix = basicType == SoundType.SFX ? sfxPath : bgmPath;
            string finalPath = $"{path}{prefix}{typeName}{enumNum}_{(TEnum)Enum.ToObject(typeof(TEnum), enumNum)}";
            return finalPath;
        }

        private async UniTask<AudioClip> _LoadClipInternal(string resourcePath) {
            switch (load) {
            case DataLoadType.Resources:
                return Resources.Load<AudioClip>(resourcePath);

            case DataLoadType.Addressable:
                // (실제 Addressable key가 resourcePath인지, id인지 등 프로젝트 규약 필요)
                // return await AddressableManager.instance.Load<AudioClip>(resourcePath);
                break;

            case DataLoadType.Local:
                // return await DataManager.LoadAsset<AudioClip>(resourcePath);
                break;

            case DataLoadType.Server:
                // 서버 스트리밍 케이스
                break;
            }

            return null;
        }
    }
}

#if UNITY_EDITOR
/* - 개발 로그 -
 * ==========================================================
 * @Jason <사운드 리스트 관리>
 * Dictionary를 통해 수많은 사운드 리소스를 필요에 따라 O(1)의 속도로 빠르게 접근할 필요가 있다고 생각하여 작성하였습니다.
 * Serializable Dictionary를 통해 인스펙터 GUI에서 데이터 포맷을 관리하기 쉽게 만드는 시도를 하였습니다.
 * 하지만 현재까지 리서치한 Serializable Dictionary 방법들은 사실상 리스트 2개를 사용한 선형구조를 눈 속임으로 사용하는 것이 었습니다.
 * 하여 충분히 필요에 따라 내부 로직을 추후에 변경하여도 상관이 없습니다.
 * ==========================================================
 * @Jason <종속성 체크>
 * 현재 사운드 클립은 SoundUnit 컨포넌트가 존재하는 경우에만 추가되고 해당 컨포넌트가 파괴되면 연관된 사운드 클립을 제거하는 방식입니다.
 * 이러한 방식은 동일한 사운드 크립을 사용하는 유닛이 중복되어 생성될 경우, 하나의 유닛이 파괴될 때, 공통으로 사용하는 클립이 제거되는 문제가 발생할 수 있습니다. 하여 종속성을 카운트하는 로직을 넣기로 하였습니다.
 * 여러 접근 방식이 있었지만, 딕셔너리 내부에 종속되는 유닛의 수를 카운트 할 수 있는 클래스를 따로 만드는 것이 가장 가독성이 좋을 것이라 생각하여 SoundItem 클래스를 만들었습니다.
 * ==========================================================
 * @Jason <> 2025.10.30
 */
#endif