namespace HUtil.AssetHandler.Check {
    public interface IAssetChecker<TKey, TAsset> {
        bool CanLoad(TKey key);
        bool IsValid(TKey key, TAsset asset);
    }
}
