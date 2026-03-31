using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine.Assertions;

namespace HUtil.AssetHandler.Load {
    public sealed class SharedAssetLoadGate<TKey, TAsset> : IAssetLoadGate<TKey, TAsset> {
        #region Private - Fields
        readonly Dictionary<TKey, UniTask<TAsset>> loadingTable = new();
        #endregion

        #region Public - Run
        public async UniTask<TAsset> RunAsync(TKey key, Func<UniTask<TAsset>> factory) {
            Assert.IsNotNull(factory, "[SharedAssetLoadGate] factory is null.");

            if (loadingTable.TryGetValue(key, out var runningTask)) {
                return await runningTask;
            }

            var newTask = factory.Invoke();
            loadingTable[key] = newTask;

            try {
                return await newTask;
            }
            finally {
                loadingTable.Remove(key);
            }
        }
        #endregion
    }
}
