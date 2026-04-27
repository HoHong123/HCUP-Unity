using UnityEngine;
using HDiagnosis.HDebug;
using HGame.Sound;

#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * Audio.SoundManager의 레거시 enum, int 재생 래퍼 partial 스크립트입니다.
 *
 * 주의사항 ::
 * 1. 신규 코드의 기본 재생 API는 string token 경로입니다.
 * 2. legacy 재생은 내부적으로 신규 매니저의 로드 상태를 그대로 사용합니다.
 * =========================================================
 */
#endif

namespace HGame.Audio {
    public sealed partial class SoundManager {
        #region Legacy Support
        public void Play(AudioClips clip) => Play((int)clip);
        public void PlayUI(AudioClips clip) => PlayUI((int)clip);
        public void Play3D(AudioClips clip, Transform parent) => Play3D((int)clip, parent);
        public void Play3D(AudioClips clip, Vector3 worldPos) => Play3D((int)clip, worldPos);
        public void PlayBGM(AudioClips clip, bool ignoreSameClip = true) => PlayBGM((int)clip, ignoreSameClip);

        public void Play(int uid) {
            if (!_TryGetLoadedClip(uid, out var clip)) return;
            sfxAudio.PlayOneShot(clip);
        }

        public void PlayUI(int uid) {
            if (!_TryGetLoadedClip(uid, out var clip)) return;
            uiAudio.PlayOneShot(clip);
        }

        public void Play3D(int uid, Transform parent) {
            if (!_TryGetLoadedClip(uid, out var clip)) return;
            spatialPool.PlayAt(clip, parent, sfxAudio.volume);
        }

        public void Play3D(int uid, Vector3 worldPos) {
            if (!_TryGetLoadedClip(uid, out var clip)) return;
            spatialPool.PlayAt(clip, worldPos, sfxAudio.volume);
        }

        public void PlayBGM(int uid, bool ignoreSameClip = true) {
            if (!_TryGetLoadedClip(uid, out var clip)) return;
            if (ignoreSameClip && bgmAudio.isPlaying && bgmAudio.clip == clip) return;

            bgmAudio.clip = clip;
            bgmAudio.Play();
        }

        private bool _TryGetLoadedClip(int uid, out AudioClip clip) {
            if (clipRepository.TryGet(uid, out clip) && clip) return true;

#if UNITY_EDITOR
            HDebug.StackTraceError($"[Audio.SoundManager] Clip not loaded yet. Prewarm required. uid={uid}", 10);
#endif
            clip = null;
            return false;
        }
        #endregion
    }
}

#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * 주요 기능 ::
 * 1. AudioClips enum과 int uid 재생 래퍼를 제공합니다.
 * 2. UI, 3D, BGM 재생의 레거시 진입점을 유지합니다.
 *
 * 사용법 ::
 * 1. 구형 호출부를 즉시 교체할 수 없을 때 사용합니다.
 * 2. 가능하면 신규 token API로 점진 이전합니다.
 *
 * 이벤트 ::
 * 1. 별도의 이벤트는 없습니다.
 * 2. 실제 재생과 로딩 검증은 본체 SoundManager가 담당합니다.
 *
 * 기타 ::
 * 1. 레거시 지원 범위를 partial 파일로 명확히 분리합니다.
 * 2. 신규 시스템 정착 후 제거 대상을 판단하기 쉽도록 구성되어 있습니다.
 * =========================================================
 */
#endif
