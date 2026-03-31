namespace HUtil.AssetHandler.Load {
    public interface IAssetReleasableLoader<TKey, TAsset> : IAssetLoader<TKey, TAsset> {
        bool Release(TKey key);
        void ReleaseAll();
    }
}
