using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Util.UI.Entity;
using Util.OdinCompat;

namespace Util.UI.ToggleUI {
    [RequireComponent(typeof(Toggle))]
    public class ScaleOnSelectToggle : BaseCustomToggle {
        [HeaderOrTitle("Targets")]
        [SerializeField]
        ScalingUiEntity[] targets;


        public override void OnToggleActive(bool isOn) {
            if (ActivateOnSelect) _Scale(isOn);
        }
        public override void OnPointerDown(PointerEventData eventData) {
            if (ActivateOnPointerDown) _Scale(true);
        }
        public override void OnPointerUp(PointerEventData eventData) {
            if (ActivateOnPointerUp) _Scale(false);
        }


        private void _Scale(bool isOn) {
            foreach (var target in targets) {
                if (isOn) {
                    target.ChangeScale();
                }
                else {
                    target.Reset();
                }
            }
        }
    }
}