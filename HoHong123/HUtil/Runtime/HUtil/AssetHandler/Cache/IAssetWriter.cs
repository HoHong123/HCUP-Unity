namespace HUtil.AssetHandler.Cache {
    public interface IAssetWriter<TKey, TAsset> {
        bool Save(TKey key, TAsset asset);
    }
}
