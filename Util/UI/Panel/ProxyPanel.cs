#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * 특정 UI가 유저 상호작용이 필요하지만, 매번 컨트롤러를 새로 생성하기 번거롭기에 작성한 스크립트입니다.
 * 
 * ** 사용방법 **
 * 1. 명칭 그대로 프록시 역할을 하며, 상호작용이 필요한 UI에 컴포넌트로 넣습니다.
 * 2. 원하는 이벤트에 동작할 함수들을 대리자에 등록합니다.
 * 3. 중복 드래그 방지 기능이 필요하다면 'SetAutoDragCheck'함수로 해당 프록시 스크립트를 등록하면 됩니다.
 * =========================================================
 */
#endif

using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Util.UI.Panel {
    public class ProxyPanel : MonoBehaviour,
        IBeginDragHandler, IDragHandler, IEndDragHandler,
        IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler,
        IPointerClickHandler, IPointerUpHandler, IPointerDownHandler,
        IDropHandler {

        public event Action<PointerEventData> BeginDragEvent;
        public event Action<PointerEventData> EndDragEvent;
        public event Action<PointerEventData> OnDragEvent;
        public event Action<PointerEventData> OnDropEvent;
        public event Action<PointerEventData> PointerEnterEvent;
        public event Action<PointerEventData> PointerExitEvent;
        public event Action<PointerEventData> PointerMoveEvent;
        public event Action<PointerEventData> PointerClickEvent;
        public event Action<PointerEventData> PointerUpEvent;
        public event Action<PointerEventData> PointerDownEvent;

        public void SetAutoDragCheck(object proxyOwner) {
            BeginDragEvent += (eventData) => UiEvent.LockDrag(proxyOwner);
            EndDragEvent += (eventData) => UiEvent.UnlockDrag(proxyOwner);
        }

        public void OnBeginDrag(PointerEventData eventData) => BeginDragEvent?.Invoke(eventData);
        public void OnEndDrag(PointerEventData eventData) => EndDragEvent?.Invoke(eventData);
        public void OnDrag(PointerEventData eventData) => OnDragEvent?.Invoke(eventData);
        public void OnDrop(PointerEventData eventData) => OnDropEvent?.Invoke(eventData);
        public void OnPointerEnter(PointerEventData eventData) => PointerEnterEvent?.Invoke(eventData);
        public void OnPointerExit(PointerEventData eventData) => PointerExitEvent?.Invoke(eventData);
        public void OnPointerMove(PointerEventData eventData) => PointerMoveEvent?.Invoke(eventData);
        public void OnPointerClick(PointerEventData eventData) => PointerClickEvent?.Invoke(eventData);
        public void OnPointerUp(PointerEventData eventData) => PointerUpEvent?.Invoke(eventData);
        public void OnPointerDown(PointerEventData eventData) => PointerDownEvent?.Invoke(eventData);
    }
}
