using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace HUI.Panel {
    public class HoveringPanel : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {
        Coroutine hoverRoutine;

        public float Duration { get; set; } = 2f;

        public event Action OnPointerEnterEvent;
        public event Action OnPointerExitEvent;
        public event Action<Vector2> OnHoveringComplete;


        private void OnEnable() {
            OnPointerEnterEvent += _StartHoverCheck;
            OnPointerExitEvent += _StopHoverCheck;
        }

        private void OnDisable() {
            OnPointerEnterEvent -= _StartHoverCheck;
            OnPointerExitEvent -= _StopHoverCheck;
            _StopHoverCheck();
        }


        public void OnPointerEnter(PointerEventData eventData) {
            OnPointerEnterEvent?.Invoke();
        }

        public void OnPointerExit(PointerEventData eventData) {
            OnPointerExitEvent?.Invoke();
        }


        private void _StartHoverCheck() {
            _StopHoverCheck();
            hoverRoutine = StartCoroutine(_HoverRoutine());
        }

        private void _StopHoverCheck() {
            if (hoverRoutine != null) {
                StopCoroutine(hoverRoutine);
                hoverRoutine = null;
            }
        }

        private IEnumerator _HoverRoutine() {
            yield return new WaitForSeconds(Duration);
            OnHoveringComplete?.Invoke(transform.position);
        }
    }
}
