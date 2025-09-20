using UnityEngine;
using Util.UI.Entity;
using Util.OdinCompat;

namespace Util.UI.ButtonUI {
    [RequireComponent(typeof(DelegateButton))]
    public class ColorOnPressButton : BaseOnPressButton {
        [HeaderOrTitle("Target")]
        [SerializeField]
        ColorUiEntity[] targets;

        public ColorUiEntity[] ColorEntity => targets;


        public override void OnPointDown() {
            foreach (var target in targets)
                target.Dye();
        }

        public override void OnPointUp() {
            foreach (var target in targets)
                target.Reset();
        }
    }
}
