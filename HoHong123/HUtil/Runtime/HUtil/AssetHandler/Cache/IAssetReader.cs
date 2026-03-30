namespace HUtil.AssetHandler.Cache {
    public interface IAssetReader<TKey, TAsset> {
        bool TryGet(TKey key, out TAsset asset);
    }
}
