using Cysharp.Threading.Tasks;
using HUtil.AssetHandler.Data;

namespace HUtil.AssetHandler.Provider {
    public interface IAssetProvider<TKey, TAsset> {
        UniTask<TAsset> GetAsync(AssetRequest<TKey> request);
        UniTask<TAsset> GetAsync(
            TKey key,
            AssetLoadMode loadMode,
            AssetFetchMode fetchMode = AssetFetchMode.CacheFirst,
            object owner = null);

        bool TryGet(TKey key, out TAsset asset);

        bool Release(TKey key);
        void ReleaseAll();
        void ClearCache();

        UniTask ClearStoreAsync();
    }
}
