using UnityEngine;
using UnityEngine.EventSystems;
using Util.OdinCompat;
using Util.UI.Entity;

namespace Util.UI.ToggleUI {
    public class ColorOnSelectToggle : BaseCustomToggle {
        [HeaderOrTitle("Targets")]
        [SerializeField]
        ColorUiEntity[] targets;

        public ColorUiEntity[] ColorEntity => targets;


        public override void OnToggleActive(bool isOn) {
            if (ActivateOnSelect) _Dye(isOn);
        }
        public override void OnPointerDown(PointerEventData eventData) {
            if (ActivateOnPointerDown) _Dye(true);
        }
        public override void OnPointerUp(PointerEventData eventData) {
            if (ActivateOnPointerUp) _Dye(false);
        }


        private void _Dye(bool isOn) {
            foreach (var target in targets) {
                if (isOn)
                    target.Dye();
                else
                    target.Reset();
            }
        }
    }
}