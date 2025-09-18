using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace Util.UI.ToggleUI {
    [RequireComponent(typeof(Toggle))]
    public class OnOffDelegatorToggle : BaseCustomToggle {
        public UnityEvent OnToggledOn = new();
        public UnityEvent OnToggledOff = new();


        public override void OnToggleActive(bool isOn) {
            if (isOn)
                OnToggledOn?.Invoke();
            else
                OnToggledOff?.Invoke();
        }
        public override void OnPointerDown(PointerEventData eventData) {}
        public override void OnPointerUp(PointerEventData eventData) {}


        private void _OnOff(bool isOn) {
            if (isOn)
                OnToggledOn?.Invoke();
            else
                OnToggledOff?.Invoke();
        }
    }
}