using UnityEngine;
using UnityEngine.UI;

namespace HGame.Sound.AddOn {
    [RequireComponent(typeof(Toggle))]
    public class ToggleSfxAddon : BaseSfxAddon {
        #region Field
        Toggle toggle;
        #endregion

        #region Unity Life Cycle
        private void Start() {
            toggle = GetComponent<Toggle>();
            UnityEngine.Assertions.Assert.IsNotNull(toggle);

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
