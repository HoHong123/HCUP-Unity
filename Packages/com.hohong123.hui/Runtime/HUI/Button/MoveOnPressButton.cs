using UnityEngine;
using Sirenix.OdinInspector;
using HUtil.UI.Entity;

namespace HUtil.UI.ButtonUI {
    [RequireComponent(typeof(DelegateButton))]
    public class MoveOnPressButton : BaseOnPressButton {
        [Title("Target")]
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
