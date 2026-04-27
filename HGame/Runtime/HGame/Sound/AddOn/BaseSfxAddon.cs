using UnityEngine;
using HInspector;

namespace HGame.Sound.AddOn {
    public class BaseSfxAddon : MonoBehaviour {
        #region Field
        [HTitle("Sound Policy")]
        [SerializeField]
        protected bool useOverride = false;
        [HShowIf(nameof(useOverride))]
        [SerializeField]
        protected int overrideClickUid = 0;
        #endregion

        #region Protected - Handler
        protected virtual void _HandleClick() {
            if (!SoundManager.HasInstance) return;

            int uid = useOverride ? overrideClickUid : SoundManager.DEFAULT_CLICK_UID;
            if (uid <= 0) return;

            SoundManager.Instance.PlayUI(uid);
        }
        #endregion
    }
}
