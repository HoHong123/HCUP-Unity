#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * 포인터가 UI 위에 호버링 상태라면 동작하는 패널 기능 스크립트입니다.
 * 
 * ** 사용방법 **
 * 1. 해당 스크립트를 호버링 기능이 필요한 UI에 컴포넌트로 등록합니다.
 * 2. 해당 컴포넌트를 사용할 컨트롤러가 '이벤트 시작/취소/종료'에 실행될 이벤트를 대리자에 등록합니다.
 * =========================================================
 */
#endif

using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Util.UI.Panel {
    public class HoveringPanel : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {
        CancellationTokenSource hoverCts;

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


        public void OnPointerEnter(PointerEventData eventData) => OnPointerEnterEvent?.Invoke();
        public void OnPointerExit(PointerEventData eventData) => OnPointerExitEvent?.Invoke();


        private void _StartHoverCheck() {
            _StopHoverCheck();
            hoverCts = new CancellationTokenSource();
            _HoverAsync(hoverCts.Token).Forget();
        }

        private void _StopHoverCheck() {
            if (hoverCts == null) return;
            hoverCts.Cancel();
            hoverCts.Dispose();
            hoverCts = null;
        }

        private async UniTaskVoid _HoverAsync(CancellationToken ct) {
            try {
                await UniTask.Delay(
                    TimeSpan.FromSeconds(Duration),
                    DelayType.DeltaTime,
                    PlayerLoopTiming.Update,
                    ct);

                if (!ct.IsCancellationRequested)
                    OnHoveringComplete?.Invoke((Vector2)transform.position);
            }
            catch (OperationCanceledException) {
                // Cancel
            }
        }
    }
}
