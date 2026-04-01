#if UNITY_EDITOR
/* =========================================================
 * Toggle 상태 변경 이벤트를 위임받기 위한 인터페이스입니다.
 *
 * 목적 ::
 * Toggle 상태 변경 시 외부 로직을 연결하기 위한 공통 인터페이스입니다.
 * =========================================================
 */
#endif

namespace HUI.ToggleUI {
    public interface IDelegateToggle {
        public void OnToggleActive(bool isOn);
    }
}