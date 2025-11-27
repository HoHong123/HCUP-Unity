namespace HUtil.Web {
    public class BaseExternalReceiver : UnityEngine.MonoBehaviour, IWebReceiver {
        public virtual void ReceiveMessage() {}
        public virtual void ReceiveString(string message) {}
    }
}