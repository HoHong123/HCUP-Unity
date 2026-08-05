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
    }
}
