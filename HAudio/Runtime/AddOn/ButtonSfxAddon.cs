using UnityEngine;
using HUI.ButtonUI;

namespace HAudio.AddOn {
    [RequireComponent(typeof(DelegateButton))]
    public sealed class ButtonSfxAddon : BaseSfxAddon {
        #region Field
        DelegateButton btn;
        #endregion

        #region Unity Life Cycle
        private void Start() {
            btn = GetComponent<DelegateButton>();
            if (btn == null) {
                HDiagnosis.Logger.HLogger.Error($"[ButtonSfxAddon] DelegateButton not found on '{gameObject.name}'.", gameObject);
                return;
            }

            btn.OnPointUp -= _HandleClick;
            btn.OnPointUp += _HandleClick;
        }

        private void OnDestroy() {
            if (btn != null) btn.OnPointUp -= _HandleClick;
        }
        #endregion

        #region Protected - Handler
        protected override void _HandleClick() {
            if (!AudioManager.HasInstance) return;

            if (overrideClickUid != 0) {
                AudioManager.Instance.PlayUI(overrideClickUid);
                return;
            }

            AudioManager.Instance.PlayClick();
        }
        #endregion
    }
}

#if UNITY_EDITOR
/* =============================================================================
 *  Dev Log
 * =============================================================================
 * @Jason - PKH 2026.08.07 _HandleClick 구현 이전 (BaseSfxAddon abstract 전환)
 *
 * # 변경
 * - `BaseSfxAddon._HandleClick` 이 `protected abstract` 로 바뀌면서, 기존 base 기본
 *   구현(overrideClickUid 우선 → PlayUI, 없으면 PlayClick)을 이 파일로 그대로 이전.
 *
 * # 이유
 * - `ButtonSfxAddon` 은 UI 버튼이므로 `PlayUI`/`PlayClick` 재생축이 맞는 파생 클래스다.
 *   base 가 더 이상 이 축을 강제하지 않으므로 여기서 명시적으로 구현한다.
 *
 * # 결과
 * - 동작 변경 없음(기존 base 구현과 완전히 동일한 로직).
 *
 * =============================================================================
 */
#endif
