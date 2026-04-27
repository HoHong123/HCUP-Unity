#if UNITY_EDITOR
/* =========================================================
 * Toggle 상태를 UnityEvent로 전달하는 Delegator 컴포넌트입니다.
 *
 * 목적 ::
 * Toggle 상태를 Inspector에서 직접 이벤트로 연결하기 위함입니다.
 * =========================================================
 */
#endif

using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace HUI.ToggleUI {
    public class OnOffDelegatorToggle : BaseCustomToggle {
        public UnityEvent OnToggledOn = new();
        public UnityEvent OnToggledOff = new();


        public override void OnToggleActive(bool isOn, bool immediate) {
            _OnOff(isOn);
        }
        public override void OnPointerDown(PointerEventData eventData) {}
        public override void OnPointerUp(PointerEventData eventData) {}


        private void _OnOff(bool isOn) {
            if (isOn)   OnToggledOn?.Invoke();
            else        OnToggledOff?.Invoke();
        }
    }
}

#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 *
 * 주요 기능 ::
 * Toggle On → OnToggledOn Invoke
 * Toggle Off → OnToggledOff Invoke
 *
 * 사용법 ::
 * Inspector에서 UnityEvent에 원하는 함수 연결
 *
 * 기타 ::
 * Toggle 상태 전달 전용 Delegator 클래스입니다.
 * =========================================================
 */
#endif