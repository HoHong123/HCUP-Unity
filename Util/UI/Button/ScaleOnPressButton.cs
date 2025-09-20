using UnityEngine;
using Util.UI.Entity;
using Util.OdinCompat;

namespace Util.UI.ButtonUI {
    [RequireComponent(typeof(DelegateButton))]
    public class ScaleOnPressButton : BaseOnPressButton {
        [HeaderOrTitle("Target")]
        [SerializeField]
        ScalingUiEntity[] targets;


        public override void OnPointDown() {
            foreach (var target in targets)
                target.ChangeScale();
        }

        public override void OnPointUp() {
            foreach (var target in targets)
                target.Reset();
        }
    }
}
