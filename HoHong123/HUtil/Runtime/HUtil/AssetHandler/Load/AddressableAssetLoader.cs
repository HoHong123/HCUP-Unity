using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using HUtil.AssetHandler.Data;

namespace HUtil.AssetHandler.Load {
    public sealed class AddressableAssetLoader<TAsset> : IAssetReleasableLoader<string, TAsset>
        where TAsset : Object {
        #region Fields
        readonly Dictionary<string, AsyncOperationHandle<TAsset>> handleTable = new();
        #endregion

        #region Properties
        public AssetLoadMode LoadMode => AssetLoadMode.Addressable;
        #endregion

        #region Public - Load
        public async UniTask<TAsset> LoadAsync(string key) {
            var normalizedKey = _NormalizeKey(key);
            if (string.IsNullOrWhiteSpace(normalizedKey)) return null;

            if (handleTable.TryGetValue(normalizedKey, out var cachedHandle)) {
                if (cachedHandle.IsValid()) return cachedHandle.Result;
                handleTable.Remove(normalizedKey);
            }

            var handle = Addressables.LoadAssetAsync<TAsset>(normalizedKey);
            await handle.ToUniTask();

            if (handle.Status != AsyncOperationStatus.Succeeded) {
                if (handle.IsValid()) Addressables.Release(handle);
                return null;
            }

            handleTable[normalizedKey] = handle;
            return handle.Result;
        }
        #endregion

        #region Public - Release
        public bool Release(string key) {
            var normalizedKey = _NormalizeKey(key);
            if (string.IsNullOrWhiteSpace(normalizedKey)) {
                return false;
            }

            if (!handleTable.TryGetValue(normalizedKey, out var handle)) {
                return false;
            }

            if (handle.IsValid()) {
                Addressables.Release(handle);
            }

            handleTable.Remove(normalizedKey);
            return true;
        }

        public void ReleaseAll() {
            foreach (var handle in handleTable.Values) {
                if (handle.IsValid()) Addressables.Release(handle);
            }

            handleTable.Clear();
        }
        #endregion

        #region Private - Normalize
        private string _NormalizeKey(string key) {
            if (string.IsNullOrWhiteSpace(key)) return string.Empty;
            return key.Trim();
        }
        #endregion
    }
}
