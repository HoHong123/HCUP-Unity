#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * 외부 메시지 수신 인터페이스입니다.
 * Web 또는 Native 환경에서 전달되는 메시지를 Unity 내부 시스템에서 처리하기 위해 사용됩니다.
 *
 * 제공 함수 ::
 * ReceiveMessage
 * ReceiveString
 * =========================================================
 */
#endif

namespace HUtil.Web {
    public interface IWebReceiver {
        void ReceiveMessage();
        void ReceiveString(string message);
    }
}