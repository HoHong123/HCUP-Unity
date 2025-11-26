namespace HUtil.Web {
    public interface IWebReceiver {
        void ReceiveMessage();
        void ReceiveString(string message);
    }
}