#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * 버튼 이벤트 처리 방식을 정의하는 열거형입니다.
 * =========================================================
 */
#endif

namespace HUI.ButtonUI {
    public enum ButtonEventMode : byte {
        UsePress = 1 << 0,
        UseInteraction = 1 << 1,
    }
}