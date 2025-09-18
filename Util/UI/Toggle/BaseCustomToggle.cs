using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Sirenix.OdinInspector;

namespace Util.UI.ToggleUI {
    public abstract class BaseCustomToggle : MonoBehaviour, IDelegateToggle, IPointerDownHandler, IPointerUpHandler {
        [Title("Event Timing")]
        [SerializeField]
        protected bool ActivateOnSelect = true;
        [SerializeField]
        protected bool ActivateOnPointerDown = false;
        [SerializeField]
        protected bool ActivateOnPointerUp = false;


        private void Awake() {
            Toggle toggle = GetComponent<Toggle>();
            toggle.onValueChanged.AddListener(OnToggleActive);
        }


        public abstract void OnToggleActive(bool isOn);
        public abstract void OnPointerDown(PointerEventData eventData);
        public abstract void OnPointerUp(PointerEventData eventData);
    }
}