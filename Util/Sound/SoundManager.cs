using System;
using UnityEngine;
using UnityEngine.Audio;
using Util.Core;
using Util.Logger;
using Util.OdinCompat;

namespace Util.Sound {
    public partial class SoundManager : SingletonBehaviour<SoundManager> {
        [Serializable]
        public class SoundItem {
            public int Dependency;
            public AudioClip Clip;

            public SoundItem(int _dependency, AudioClip _clip) {
                Dependency = _dependency;
                Clip = _clip;
            }
        }

        [HeaderOrTitle("Meta Data")]
        [SerializeField]
        string path = "Sounds/";
        [SerializeField]
        string sfxPath = "SFX/";
        [SerializeField]
        string bgmPath = "BGM/";

        [HeaderOrTitle("Audio Mixer")]
        [SerializeField]
        AudioMixer audioMix;

        [HeaderOrTitle("Audio Sources")]
        [SerializeField]
        AudioSource sfxAudio;
        [SerializeField]
        AudioSource bgmAudio;

#if !ODIN_INSPECTOR
        Dictionary<int, SoundItem> soundDic = new();
#endif

        public void SetSoundUnit(SoundContainer container) {
            foreach (var clip in container.Clips) {
                _LoadClip(clip.Id);
            }
        }

        public void RemoveSoundUnit(SoundContainer container) {
            foreach (var clip in container.Clips) {
                if (!soundDic.ContainsKey(clip.Id) || --soundDic[clip.Id].Dependency > 0)
                    continue;
                soundDic.Remove(clip.Id);
            }
        }


        public void PlayClickSound() => PlayOneShot(SFXList.Click); // 가장 많이 사용되는 사운드에 대해 특수 케이스로 인식하여 전용 함수를 작성
        public void PlayOneShot(SFXList flag) => PlayOneShot((int)flag);
        public void PlayOneShot(int id) {
            if (!_CanPlayClip(id)) return;
            sfxAudio.PlayOneShot(soundDic[id].Clip);
        }

        public void PlayBGM(BGMList _index) {
            int flag = (int)_index;
            if (!_CanPlayClip(flag)) return;
            bgmAudio.clip = soundDic[flag].Clip;
            bgmAudio.Play();
        }


        private bool _CanPlayClip(int id) {
            if (!soundDic.ContainsKey(id)) {
                HLogger.Error($"[SoundManager] Cannot play sound. Does not have ID({id}) sound data.");
                return false;
            }
            return true;
        }

        private async void _LoadClip(int id, SoundType type = SoundType.SFX) {
            if (soundDic.ContainsKey(id)) {
                soundDic[id].Dependency++;
                return;
            }

            soundDic.Add(id, new(1, null));

            string resourcePath = path + (type == SoundType.SFX ? sfxPath : bgmPath) + id.ToString();
            var clip = Resources.Load<AudioClip>(resourcePath);
            // Type 1 :: Load from resources.
            //var clip = await DataManager.LoadAsset<AudioClip>(resourcePath);
            // Type 2 :: Load addressable.
            // TODO :: Need refactoring when addressable is required
            //var clip = await AddressableManager.instance.Load<AudioClip>(resourcePath);

            soundDic[id].Clip = clip;
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
 */
#endif