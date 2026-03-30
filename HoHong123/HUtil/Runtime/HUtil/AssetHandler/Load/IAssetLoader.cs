using Cysharp.Threading.Tasks;
using HUtil.AssetHandler.Data;

namespace HUtil.AssetHandler.Load {
    public interface IAssetLoader<TKey, TAsset> {
        AssetLoadMode LoadMode { get; }
        UniTask<TAsset> LoadAsync(TKey key);
    }
}
