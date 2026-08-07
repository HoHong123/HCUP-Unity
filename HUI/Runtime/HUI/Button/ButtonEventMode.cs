#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * 버튼 이벤트 처리 방식을 정의하는 열거형입니다.
 *
 * 주의사항 ::
 * ColorOnPressButton / EnableOnPressButton 모두 switch 문으로 배타적 단일 값만 소비한다.
 * [Flags] 조합을 지원하지 않으므로 값도 비트 시프트가 아닌 순번으로 명시한다.
 * =========================================================
 */
#endif

namespace HUI.ButtonUI {
    public enum ButtonEventMode : byte {
        UsePress = 1,
        UseInteraction = 2,
    }
}