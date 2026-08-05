using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace HUI.Panel {
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

        object autoDragOwner;

        /// <summary> 드래그 시작/종료에 UiEvent 드래그 잠금을 자동 연결. 재호출 시 owner 만 교체된다. </summary>
        public void SetAutoDragCheck(object proxyOwner) {
            // 익명 람다 구독은 해제 수단이 없고 재호출마다 누적된다 — 명명 핸들러 1회 구독으로 고정.
            if (autoDragOwner == null) {
                BeginDragEvent += _OnAutoDragBegin;
                EndDragEvent += _OnAutoDragEnd;
            }
            autoDragOwner = proxyOwner;
        }

        public void ClearAutoDragCheck() {
            if (autoDragOwner == null) return;
            BeginDragEvent -= _OnAutoDragBegin;
            EndDragEvent -= _OnAutoDragEnd;
            autoDragOwner = null;
        }

        private void _OnAutoDragBegin(PointerEventData eventData) => UiEvent.LockDrag(autoDragOwner);
        private void _OnAutoDragEnd(PointerEventData eventData) => UiEvent.UnlockDrag(autoDragOwner);

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
