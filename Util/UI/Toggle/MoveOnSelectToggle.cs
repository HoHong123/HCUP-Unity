using UnityEngine;
using UnityEngine.UI;
using Sirenix.OdinInspector;
using Util.UI.Entity;
using UnityEngine.EventSystems;

namespace Util.UI.ToggleUI {
    [RequireComponent(typeof(Toggle))]
    public class MoveOnSelectToggle : BaseCustomToggle {
        [Title("Targets")]
        [SerializeField]
        MovingUiEntity[] targets;


        public override void OnToggleActive(bool isOn) {
            if (ActivateOnSelect) _Move(isOn);
        }
        public override void OnPointerDown(PointerEventData eventData) {
            if (ActivateOnPointerDown) _Move(true);
        }
        public override void OnPointerUp(PointerEventData eventData) {
            if (ActivateOnPointerUp) _Move(false);
        }


        private void _Move(bool isOn) {
            foreach (var target in targets) {
                if (isOn)
                    target.Move();
                else
                    target.Reset();
            }
        }
    }
}