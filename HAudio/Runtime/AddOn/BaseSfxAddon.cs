using UnityEngine;
using UnityEngine.Serialization;
using HInspector;

namespace HAudio.AddOn {
    public class BaseSfxAddon : MonoBehaviour {
        #region Field
        [HTitle("Sound Policy")]
        [SerializeField]
        protected bool useOverride = false;
        [HShowIf(nameof(useOverride))]
        [SerializeField, FormerlySerializedAs("overrideClickUid")]
        protected string overrideClickToken = string.Empty;
        #endregion

        #region Protected - Handler
        protected virtual void _HandleClick() {
            if (!AudioManager.HasInstance) return;

            if (useOverride) {
                if (!string.IsNullOrEmpty(overrideClickToken)) {
                    AudioManager.Instance.PlayUI(overrideClickToken);
                    return;
                }
            }

            AudioManager.Instance.PlayClick();
        }
        #endregion
    }
}
