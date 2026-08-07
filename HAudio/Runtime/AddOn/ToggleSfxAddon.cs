using UnityEngine;
using UnityEngine.UI;

namespace HAudio.AddOn {
    [RequireComponent(typeof(Toggle))]
    public class ToggleSfxAddon : BaseSfxAddon {
        #region Field
        Toggle toggle;
        #endregion

        #region Unity Life Cycle
        private void Start() {
            toggle = GetComponent<Toggle>();
            if (toggle == null) {
                HDiagnosis.Logger.HLogger.Error($"[ToggleSfxAddon] Toggle not found on '{gameObject.name}'.", gameObject);
                return;
            }

            toggle.onValueChanged.RemoveListener(_ToggleHandler);
            toggle.onValueChanged.AddListener(_ToggleHandler);
        }

        private void OnDestroy() {
            if (toggle != null) toggle.onValueChanged.RemoveListener(_ToggleHandler);
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

        #region Private - Handler
        private void _ToggleHandler(bool isOn) {
            if (!isOn) return;
            _HandleClick();
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
 * - `ToggleSfxAddon` 도 UI 토글이므로 `PlayUI`/`PlayClick` 재생축이 맞는 파생 클래스다.
 *   base 가 더 이상 이 축을 강제하지 않으므로 여기서 명시적으로 구현한다.
 *
 * # 결과
 * - 동작 변경 없음(기존 base 구현과 완전히 동일한 로직).
 *
 * =============================================================================
 */
#endif
