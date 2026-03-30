namespace HUtil.AssetHandler.Cache {
    public interface IAssetCache<TKey, TAsset> :
        IAssetReader<TKey, TAsset>,
        IAssetWriter<TKey, TAsset>,
        IAssetReleaser<TKey> {
    }
}
