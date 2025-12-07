using UnityEngine;
using Sirenix.OdinInspector;
using HUI.Entity;

namespace HUI.ButtonUI {
    public class MoveOnPressButton : BaseOnPressButton {
        [Title("Target")]
        [SerializeField]
        MovingUiEntity[] targets;


        public override void OnPointDown() {
            foreach (var target in targets) {
                target.Move();
            }
        }

        public override void OnPointUp() {
            foreach (var target in targets) {
                target.Reset();
            }
        }
    }
}
