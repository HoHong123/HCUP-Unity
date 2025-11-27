using UnityEngine;
using Sirenix.OdinInspector;
using HUtil.UI.Entity;

namespace HUtil.UI.ButtonUI {
    public class ColorOnPressButton : BaseOnPressButton {
        [Title("Target")]
        [SerializeField]
        [ListDrawerSettings]
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
