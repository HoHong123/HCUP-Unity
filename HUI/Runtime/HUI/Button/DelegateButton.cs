#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * Unity UI Button을 확장한 이벤트 기반 버튼 컴포넌트입니다.
 * 버튼 입력 상태에 따른 UI 동작을 컴포넌트 기반으로 분리하여 처리하기 위해 사용됩니다.
 * Unity Button 이벤트 외에도 추가적인 입력 이벤트를 제공하여 버튼 입력 처리 확장성을 제공합니다.
 * =========================================================
 */
#endif

using System;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace HUI.ButtonUI {
        public class DelegateButton : Button, IPointerDownHandler, IPointerUpHandler {
        #region Events
        public event Action OnPointDown;
        public event Action OnPointUp;
        public event Action OnButtonInteractive;
        public event Action OnButtonNonInteractive;
        #endregion

        #region Property
        public bool Interaction {
            get => interactable;
            set {
                interactable = value;
                if (interactable) {
                    OnButtonInteractive?.Invoke();
                }
                else {
                    OnButtonNonInteractive?.Invoke();
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