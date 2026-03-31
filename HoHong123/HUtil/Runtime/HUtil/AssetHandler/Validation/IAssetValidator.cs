namespace HUtil.AssetHandler.Validation {
    public interface IAssetValidator<TKey, TAsset> {
        bool CanLoad(TKey key);
        bool IsValid(TKey key, TAsset asset);
    }
}
