using System;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace HUI.ButtonUI {
        public class DelegateButton : Button, IPointerDownHandler, IPointerUpHandler {
        #region Events
        public event Action OnPointDown;
        public event Action OnPointUp;
        public event Action OnButtonEnabled;
        public event Action OnButtonDisabled;
        #endregion

        #region Property
        public bool Interaction {
            get => interactable;
            set {
                interactable = value;
                if (interactable) {
                    OnButtonEnabled?.Invoke();
                }
                else {
                    OnButtonDisabled?.Invoke();
                }
            }
        }
        #endregion

        #region Handler
        public override void OnPointerDown(PointerEventData eventData) {
            base.OnPointerDown(eventData);
            if (interactable) OnPointDown?.Invoke();
        }

        public override void OnPointerUp(PointerEventData eventData) {
            base.OnPointerUp(eventData);
            if (interactable) OnPointUp?.Invoke();
        }
        #endregion
    }
}


/*
 * @Jason
 * 델리게이트 버튼 스크립트는 버튼이 누르고 때어낼때 이벤트 발생.
 * 필요에 따라 원하는 타이밍에 이벤트 기능 추가 가능. (Highlighted, Hovering, Disabled... etc)
 */