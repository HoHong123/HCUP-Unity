#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * DelegateButton 입력 이벤트를 처리하기 위한 인터페이스입니다.
 * 버튼 입력 이벤트 처리 로직을 컴포넌트 단위로 구현하기 위해 사용됩니다.
 * =========================================================
 */
#endif

namespace HUI.ButtonUI {
    public interface IDelegateButton {
        public void OnPointDown();
        public void OnPointUp();
    }
}