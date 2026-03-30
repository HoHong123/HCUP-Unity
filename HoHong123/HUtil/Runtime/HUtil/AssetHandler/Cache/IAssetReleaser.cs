namespace HUtil.AssetHandler.Cache {
    public interface IAssetReleaser<TKey> {
        bool Release(TKey key);
        void ReleaseAll();
        void Clear();
    }
}
