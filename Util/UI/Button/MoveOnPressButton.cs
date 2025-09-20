using UnityEngine;
using Util.OdinCompat;
using Util.UI.Entity;

namespace Util.UI.ButtonUI {
    [RequireComponent(typeof(DelegateButton))]
    public class MoveOnPressButton : BaseOnPressButton {
        [HeaderOrTitle("Target")]
        [SerializeField]
        MovingUiEntity[] targets;


        public override void OnPointDown() {
            foreach (var target in targets)
                target.Move();
        }

        public override void OnPointUp() {
            foreach (var target in targets)
                target.Reset();
        }
    }
}
