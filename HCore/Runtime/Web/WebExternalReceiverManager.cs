#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * 외부 메시지 수신 시스템을 관리하는 매니저입니다.
 * WebGL 또는 외부 플랫폼과의 메시지 통신을 중앙 관리하기 위해 사용됩니다.
 * Singleton 기반으로 동작하며 등록된 Receiver들에게 메시지를 전달합니다.
 *
 * 기능 ::
 * - IWebReceiver 등록 / 해제
 * - 외부 메시지 이벤트 전달
 * =========================================================
 */
#endif

namespace HCore.Web {
    public class WebExternalReceiverManager : HCore.SingletonBehaviour<WebExternalReceiverManager> {
        #region Fields
        [UnityEngine.SerializeField]
        BaseExternalReceiver[] receivers;
        #endregion

        #region Events
        public event System.Action OnReceiveMessage;
        public event System.Action<string> OnReceiveString;
        #endregion

        #region Public - Receive Message
        public void ReceiveMessage() => OnReceiveMessage?.Invoke();
        public void ReceiveString(string message) {
            OnReceiveString?.Invoke(message);
        }
        #endregion

        #region Private - Unity Life Cycle
        private void Start() {
            foreach (var reciver in receivers) {
                Register(reciver);
            }
        }

        private void OnDestroy() {
            foreach (var reciver in receivers) {
                Unregister(reciver);
            }
        }
        #endregion

        #region Public - Registration
        public void Register(IWebReceiver receiver) {
            Unregister(receiver);
            OnReceiveMessage += receiver.ReceiveMessage;
            OnReceiveString += receiver.ReceiveString;
        }

        public void Unregister(IWebReceiver receiver) {
            OnReceiveMessage -= receiver.ReceiveMessage;
            OnReceiveString -= receiver.ReceiveString;
        }
        #endregion
    }
}
