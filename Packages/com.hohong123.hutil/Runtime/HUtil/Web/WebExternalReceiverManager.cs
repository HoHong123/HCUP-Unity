namespace HUtil.Web {
    public class WebExternalReceiverManager : Core.SingletonBehaviour<WebExternalReceiverManager> {
        [UnityEngine.SerializeField]
        BaseExternalReceiver[] receivers;

        public event System.Action OnReceiveMessage;
        public event System.Action<string> OnReceiveString;

        public void ReceiveMessage() => OnReceiveMessage?.Invoke();
        public void ReceiveString(string message) {
            OnReceiveString?.Invoke(message);
        }


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


        public void Register(IWebReceiver receiver) {
            Unregister(receiver);
            OnReceiveMessage += receiver.ReceiveMessage;
            OnReceiveString += receiver.ReceiveString;
        }

        public void Unregister(IWebReceiver receiver) {
            OnReceiveMessage -= receiver.ReceiveMessage;
            OnReceiveString -= receiver.ReceiveString;
        }
    }
}
