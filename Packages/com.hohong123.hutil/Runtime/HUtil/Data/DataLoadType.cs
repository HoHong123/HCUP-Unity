namespace Util.Data {
    public enum DataLoadType : byte {
        Resources = 1 << 0,
        Addressable = 1 << 1,
        Local = 1 << 2,
        Server = 1 << 3,
    }
}