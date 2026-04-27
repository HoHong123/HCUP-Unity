#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * 외부 메시지를 수신하기 위한 기본 Receiver 클래스입니다.
 * 플랫폼 간 메시지 전달 시스템에서 공통 수신 인터페이스를 제공하기 위해 사용됩니다.
 *
 * 기능 ::
 * 외부 환경(Web, Native 등)에서 전달되는 메시지를 Unity 내부 로직으로 전달합니다.
 * =========================================================
 */
#endif

namespace HCore.Web {
    public class BaseExternalReceiver : UnityEngine.MonoBehaviour, IWebReceiver {
        public virtual void ReceiveMessage() {}
        public virtual void ReceiveString(string message) {}
    }
}