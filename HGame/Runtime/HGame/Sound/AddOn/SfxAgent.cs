#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * SFX 재생과 preload 호출의 게이트 클래스입니다.
 *
 * 특징 / 지원기능 ::
 * + useNewManager flag 로 AudioManager / SoundManager 라우팅 분기.
 * + token / int uid / AudioClips enum 세 가지 호출 인자 지원.
 * + Start 시점에 preload, OnDestroy 시점에 release 자동 수행.
 *
 * 주의사항 ::
 * + preloadUids 의 SfxView.Catalogs 는 인스펙터에서 미리 채워야 합니다.
 * + Prewarm 호출 중 예외 발생 시 catch 가 GameObject 이름 + 예외 타입/메시지를 stack trace 와 함께 출력합니다.
 *
 * 사용법 ::
 * + 인스펙터에서 preloadUids 에 SfxView 추가 (각 SfxView 안에 Catalogs 채움).
 * + 외부 호출은 Play / PlayUI / Play3D (token / int uid / AudioClips enum 가능).
 * =========================================================
 */
#endif

using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using HUtil.Data;
using HInspector;
using HDiagnosis.Logger;
using HDiagnosis.HDebug;

namespace HGame.Sound.AddOn {
    public class SfxAgent : MonoBehaviour, IDataSubscriber {
        #region Fields
        [HTitle("Settings")]
        [SerializeField]
        bool useNewManager = true;

        [HTitle("Clips")]
        [SerializeField]
        [HListDrawer(DefaultExpandedState = true)]
        List<SfxView> preloadUids = new();
        #endregion

        #region Properties
        public IReadOnlyList<SfxView> PreloadUids => preloadUids;
        #endregion

        #region Unity Life Cycle
        private void Start() {
            if (!_HasTargetManager()) return;
            _PrewarmViews().Forget();
        }

        private void OnDestroy() {
            if (!Application.isPlaying) return;
            if (!_HasTargetManager()) return;
            _ReleaseViews();
        }
        #endregion

        #region Public - Token API
        public void Play(string token) {
            if (!useNewManager || !Audio.AudioManager.HasInstance) return;
            Audio.AudioManager.Instance.Play(token);
        }

        public void PlayUI(string token) {
            if (!useNewManager || !Audio.AudioManager.HasInstance) return;
            Audio.AudioManager.Instance.PlayUI(token);
        }

        public void Play3D(string token, Transform parent) {
            if (!useNewManager || !Audio.AudioManager.HasInstance) return;
            Audio.AudioManager.Instance.Play3D(token, parent);
        }

        public void Play3D(string token, Vector3 worldPos) {
            if (!useNewManager || !Audio.AudioManager.HasInstance) return;
            Audio.AudioManager.Instance.Play3D(token, worldPos);
        }
        #endregion

        #region Legacy Support
        public void Play(AudioClips clip) => Play((int)clip);
        public void PlayUI(AudioClips clip) => PlayUI((int)clip);
        public void Play3D(AudioClips clip, Transform parent) => Play3D((int)clip, parent);
        public void Play3D(AudioClips clip, Vector3 worldPos) => Play3D((int)clip, worldPos);

        public void Play(int uid) {
            if (useNewManager) {
                if (!Audio.AudioManager.HasInstance) return;
                Audio.AudioManager.Instance.Play(uid);
                return;
            }

            if (!SoundManager.HasInstance) return;
            SoundManager.Instance.Play(uid);
        }

        public void PlayUI(int uid) {
            if (useNewManager) {
                if (!Audio.AudioManager.HasInstance) return;
                Audio.AudioManager.Instance.PlayUI(uid);
                return;
            }

            if (!SoundManager.HasInstance) return;
            SoundManager.Instance.PlayUI(uid);
        }

        public void Play3D(int uid, Transform parent) {
            if (useNewManager) {
                if (!Audio.AudioManager.HasInstance) return;
                Audio.AudioManager.Instance.Play3D(uid, parent);
                return;
            }

            if (!SoundManager.HasInstance) return;
            SoundManager.Instance.Play3D(uid, parent);
        }

        public void Play3D(int uid, Vector3 worldPos) {
            if (useNewManager) {
                if (!Audio.AudioManager.HasInstance) return;
                Audio.AudioManager.Instance.Play3D(uid, worldPos);
                return;
            }

            if (!SoundManager.HasInstance) return;
            SoundManager.Instance.Play3D(uid, worldPos);
        }
        #endregion

        #region Private - Manager
        private bool _HasTargetManager() {
            if (useNewManager) return Audio.AudioManager.HasInstance;
            return SoundManager.HasInstance;
        }
        #endregion

        #region Private - Prewarm
        private async UniTaskVoid _PrewarmViews() {
            try {
                if (useNewManager) {
                    foreach (var view in preloadUids) {
                        if (view == null)
                            continue;
                        await Audio.AudioManager.Instance.PrewarmSfxView(view);
                    }
                    return;
                }
            }
            catch (System.Exception ex) {
                HDebug.StackTraceError($"[AudioManager] {gameObject.name} : {ex.GetType().Name} - {ex.Message}\n{ex.StackTrace}", 20);
                HLogger.Exception(ex);
                return;
            }

            try {
                await SoundManager.Instance.PrewarmIds(preloadUids);
            }
            catch (System.Exception ex) {
                HDebug.StackTraceError($"[AudioManager] {gameObject.name} : {ex.GetType().Name} - {ex.Message}\n{ex.StackTrace}", 20);
                HLogger.Exception(ex);
            }
        }

        private void _ReleaseViews() {
            if (useNewManager) {
                foreach (var view in preloadUids) {
                    if (view == null) continue;
                    Audio.AudioManager.Instance.ReleaseSfxView(view);
                }
                return;
            }

            SoundManager.Instance.ReleaseIds(preloadUids);
        }
        #endregion
    }
}

#if UNITY_EDITOR
/* =============================================================================
 *  Dev Log
 * =============================================================================
 * @Jason - PKH 2026.05.01 _PrewarmViews catch 진단 정보 회복 + 헤더/데브로그 정본화
 *
 * # 변경
 * - _PrewarmViews 의 두 catch 블록을 `catch (System.Exception ex)` 로 변경.
 * - 출력 메시지에 ex.GetType().Name + ex.Message + ex.StackTrace 를 포함.
 * - UnityEngine.Debug.LogException(ex) 호출을 추가해 Unity 콘솔의 자동 hyperlink 와 inner stack 활용.
 * - 헤더 / 데브로그 블록 신규 추가 (정본 형식 적용).
 *
 * # 이유
 * - 기존 catch 가 예외 객체를 캡처하지 않아 [C] Player 의 SfxAgent.Start 호출 stack trace 만 노출되고 실제 throw 원인 (예외 타입 / 메시지) 이 가려진 상태였음.
 * - HDebug.StackTraceError(message, depth) 는 caller 측 stack frame 만 capture 하여 throw point 의 inner stack 이 메시지에 안 들어가는 한계가 있음. ex.StackTrace 와 LogException 으로 보완.
 * - LoadGate Preserve 패치 후에도 같은 throw 가 동일 위치에서 재현되어 throw 의 정확한 await point 식별이 우선 과제로 전환.
 *
 * =============================================================================
 */
#endif
