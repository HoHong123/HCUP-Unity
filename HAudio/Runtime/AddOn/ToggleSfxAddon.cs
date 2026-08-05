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

        #region Private - Handler
        private void _ToggleHandler(bool isOn) {
            if (!isOn) return;
            _HandleClick();
        }
        #endregion
    }
}
