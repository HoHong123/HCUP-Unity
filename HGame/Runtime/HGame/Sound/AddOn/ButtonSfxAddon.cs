using UnityEngine;
using HUI.ButtonUI;

namespace HGame.Sound.AddOn {
    [RequireComponent(typeof(DelegateButton))]
    public sealed class ButtonSfxAddon : BaseSfxAddon {
        #region Field
        DelegateButton btn;
        #endregion

        #region Unity Life Cycle
        private void Start() {
            btn = GetComponent<DelegateButton>();
            UnityEngine.Assertions.Assert.IsNotNull(btn);

            btn.OnPointUp -= _HandleClick;
            btn.OnPointUp += _HandleClick;
        }

        private void OnDestroy() {
            if (btn != null) btn.OnPointUp -= _HandleClick;
        }
        #endregion
    }
}
