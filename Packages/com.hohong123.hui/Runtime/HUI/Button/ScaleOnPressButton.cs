using UnityEngine;
using Sirenix.OdinInspector;
using HUtil.UI.Entity;

namespace HUtil.UI.ButtonUI {
    public class ScaleOnPressButton : BaseOnPressButton {
        [Title("Targets")]
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
