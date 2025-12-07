using UnityEngine;
using Sirenix.OdinInspector;
using HUI.Entity;

namespace HUI.ButtonUI {
    public class ColorOnPressButton : BaseOnPressButton {
        [Title("Target")]
        [SerializeField]
        [ListDrawerSettings]
        ColorUiEntity[] targets;

        public ColorUiEntity[] ColorEntities => targets;


        public override void OnPointDown() {
            foreach (var target in targets) {
                target.Dye();
            }
        }

        public override void OnPointUp() {
            foreach (var target in targets) {
                target.Reset();
            }
        }
    }
}
