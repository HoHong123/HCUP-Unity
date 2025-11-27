using UnityEngine;
using UnityEngine.EventSystems;
using Sirenix.OdinInspector;
using HUtil.UI.Entity;

namespace HUtil.UI.ToggleUI {
    public class ColorOnSelectToggle : BaseCustomToggle {
        [Title("Targets")]
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