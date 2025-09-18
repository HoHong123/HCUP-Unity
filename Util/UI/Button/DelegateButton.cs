#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * 델리게이트 버튼 스크립트는 버튼이 누르고 때어낼때 원하는 이벤트를 발동시키는 스크립트 입니다.
 * 필요에 따라 원하는 타이밍에 이벤트 추가가 가능합니다. (Highlighted, Hovering, Disabled... etc)
 * =========================================================
 */
#endif

using System;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Util.UI.ButtonUI {
        public class DelegateButton : Button, IPointerDownHandler, IPointerUpHandler {
        public Action OnPointDown;
        public Action OnPointUp;

        public bool Interaction {
            get => interactable;
            set {
                interactable = value;
                if (value)
                    OnPointUp?.Invoke();
                else
                    OnPointDown?.Invoke();
            }
        }

        public override void OnPointerDown(PointerEventData eventData) {
            base.OnPointerDown(eventData);
            if (interactable) OnPointDown?.Invoke();
        }

        public override void OnPointerUp(PointerEventData eventData) {
            base.OnPointerUp(eventData);
            if (interactable) OnPointUp?.Invoke();
        }
    }
}