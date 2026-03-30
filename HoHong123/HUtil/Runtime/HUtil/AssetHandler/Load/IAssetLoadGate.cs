using System;
using Cysharp.Threading.Tasks;

namespace HUtil.AssetHandler.Load {
    public interface IAssetLoadGate<TKey, TAsset> {
        UniTask<TAsset> RunAsync(TKey key, Func<UniTask<TAsset>> factory);
    }
}
