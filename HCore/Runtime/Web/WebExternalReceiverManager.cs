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
            // base.Awake() 의 중복 인스턴스 Destroy() 는 프레임 종료까지 지연된다 —
            // 자기확인 없이 진행하면 이번 프레임 Start() 가 중복 인스턴스에서도 실행되어 구독이 중복된다.
            if (Instance != this) return;
            if (receivers == null) return;

            foreach (var reciver in receivers) {
                if (reciver == null) continue;
                Register(reciver);
            }
        }

        protected override void OnDestroy() {
            if (receivers != null) {
                foreach (var reciver in receivers) {
                    if (reciver == null) continue;
                    Unregister(reciver);
                }
            }
            base.OnDestroy();
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

#if UNITY_EDITOR
/* =============================================================================
 *  Dev Log
 * =============================================================================
 * @Jason - PKH 2026.08.07 방어 가드 추가 (사용처 0건, 방어적 보강)
 *
 * # 수정
 * - receivers 배열 null 방어 추가 (미할당 시 Start/OnDestroy NRE 방지).
 * - Start() 에 자기확인(Instance != this) 가드 추가 — base Awake() 의 중복 인스턴스
 *   Destroy() 가 프레임 종료까지 지연되어, 가드 없이는 곧 파괴될 중복 인스턴스도
 *   이번 프레임 Start() 에서 구독을 수행하던 결함을 막는다.
 *
 * =============================================================================
 */
#endif
