using HUtil.AssetHandler.Data;

namespace HUtil.AssetHandler.Data {
    public readonly struct AssetRequest<TKey> {
        #region Properties
        public TKey Key { get; }
        public object Owner { get; }
        public AssetLoadMode LoadMode { get; }
        public AssetFetchMode FetchMode { get; }
        public bool HasOwner => Owner != null;
        #endregion

        #region Public - Constructors
        public AssetRequest(
            TKey key,
            AssetLoadMode loadMode,
            AssetFetchMode fetchMode = AssetFetchMode.CacheFirst,
            object owner = null) {

            Key = key;
            LoadMode = loadMode;
            FetchMode = fetchMode;
            Owner = owner;
        }
        #endregion
    }
}
