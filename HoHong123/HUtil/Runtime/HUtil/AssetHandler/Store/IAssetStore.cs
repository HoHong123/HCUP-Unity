using Cysharp.Threading.Tasks;

namespace HUtil.AssetHandler.Store {
    public interface IAssetStore<TKey, TAsset> {
        UniTask<bool> HasAsync(TKey key);
        UniTask<TAsset> LoadAsync(TKey key);
        UniTask SaveAsync(TKey key, TAsset asset);
        UniTask DeleteAsync(TKey key);
        UniTask ClearAsync();
    }
}
