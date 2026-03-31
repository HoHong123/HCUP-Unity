using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine.Assertions;
using HUtil.AssetHandler.Cache;
using HUtil.AssetHandler.Check;
using HUtil.AssetHandler.Data;
using HUtil.AssetHandler.Load;
using HUtil.AssetHandler.Store;

namespace HUtil.AssetHandler.Provider {
    public sealed class AssetProvider<TKey, TAsset> : IAssetProvider<TKey, TAsset> {
        #region Fields
        readonly IAssetCache<TKey, TAsset> assetCache;
        readonly IAssetStore<TKey, TAsset> assetStore;
        readonly IAssetChecker<TKey, TAsset> assetChecker;
        readonly IAssetLoadGate<TKey, TAsset> assetLoadGate;
        readonly Dictionary<AssetLoadMode, IAssetLoader<TKey, TAsset>> loaderTable = new();
        #endregion

        #region Public - Constructors
        public AssetProvider(
            IEnumerable<IAssetLoader<TKey, TAsset>> assetLoaders,
            IAssetCache<TKey, TAsset> assetCache,
            IAssetChecker<TKey, TAsset> assetChecker,
            IAssetLoadGate<TKey, TAsset> assetLoadGate,
            IAssetStore<TKey, TAsset> assetStore = null) {

            Assert.IsNotNull(assetLoaders, "[AssetProvider] loaders is null.");
            Assert.IsNotNull(assetCache, "[AssetProvider] cache is null.");
            Assert.IsNotNull(assetChecker, "[AssetProvider] checker is null.");
            Assert.IsNotNull(assetLoadGate, "[AssetProvider] loadGate is null.");

            this.assetCache = assetCache;
            this.assetChecker = assetChecker;
            this.assetLoadGate = assetLoadGate;
            this.assetStore = assetStore;

            foreach (var assetLoader in assetLoaders) {
                Assert.IsNotNull(assetLoader, "[AssetProvider] loader contains null.");
                loaderTable[assetLoader.LoadMode] = assetLoader;
            }

            Assert.IsTrue(loaderTable.Count > 0, "[AssetProvider] No asset loader registered.");
        }
        #endregion

        #region Public - Get
        public UniTask<TAsset> GetAsync(AssetRequest<TKey> request) {
            return _GetAsync(request);
        }

        public UniTask<TAsset> GetAsync(
            TKey key,
            AssetLoadMode loadMode,
            AssetFetchMode fetchMode = AssetFetchMode.CacheFirst,
            object owner = null) {

            var request = new AssetRequest<TKey>(
                key: key,
                loadMode: loadMode,
                fetchMode: fetchMode,
                owner: owner);

            return _GetAsync(request);
        }

        public bool TryGet(TKey key, out TAsset asset) {
            return assetCache.TryGet(key, out asset);
        }
        #endregion

        #region Public - Release
        public bool Release(TKey key) {
            return assetCache.Release(key);
        }

        public void ReleaseAll() {
            assetCache.ReleaseAll();
        }

        public void ClearCache() {
            assetCache.Clear();
        }

        public UniTask ClearStoreAsync() {
            if (assetStore == null)
                return UniTask.CompletedTask;
            return assetStore.ClearAsync();
        }
        #endregion

        #region Private - Get
        private UniTask<TAsset> _GetAsync(AssetRequest<TKey> request) {
            if (!assetChecker.CanLoad(request.Key)) {
                return UniTask.FromResult<TAsset>(default);
            }

            return assetLoadGate.RunAsync(
                request.Key,
                () => _GetByFetchModeAsync(request));
        }

        private async UniTask<TAsset> _GetByFetchModeAsync(AssetRequest<TKey> request) {
            switch (request.FetchMode) {
            case AssetFetchMode.CacheFirst:
                return await _GetCacheFirstAsync(request);
            case AssetFetchMode.LocalStoreFirst:
                return await _GetLocalStoreFirstAsync(request);
            case AssetFetchMode.LocalStoreOnly:
                return await _GetLocalStoreOnlyAsync(request);
            case AssetFetchMode.SourceFirst:
                return await _GetSourceFirstAsync(request);
            case AssetFetchMode.SourceOnly:
                return await _GetSourceOnlyAsync(request);
            default:
                Assert.IsTrue(false, $"[AssetProvider] Unsupported fetchMode. fetchMode={request.FetchMode}");
                return default;
            }
        }
        #endregion

        #region Private - Cache First
        private async UniTask<TAsset> _GetCacheFirstAsync(AssetRequest<TKey> request) {
            if (assetCache.TryGet(request.Key, out var cachedAsset))
                return cachedAsset;

            var sourceAsset = await _LoadFromSourceAsync(request);
            if (!_IsValidAsset(request.Key, sourceAsset))
                return default;

            _SaveCache(request.Key, sourceAsset);
            await _SaveStoreAsync(request.Key, sourceAsset);
            return sourceAsset;
        }
        #endregion

        #region Private - Local Store
        private async UniTask<TAsset> _GetLocalStoreFirstAsync(AssetRequest<TKey> request) {
            Assert.IsNotNull(
                assetStore,
                $"[AssetProvider] assetStore is required. fetchMode={request.FetchMode}");

            var storeAsset = await _LoadFromStoreAsync(request.Key);
            if (_IsValidAsset(request.Key, storeAsset)) {
                _SaveCache(request.Key, storeAsset);
                return storeAsset;
            }

            var sourceAsset = await _LoadFromSourceAsync(request);
            if (!_IsValidAsset(request.Key, sourceAsset))
                return default;

            _SaveCache(request.Key, sourceAsset);
            await _SaveStoreAsync(request.Key, sourceAsset);
            return sourceAsset;
        }

        private async UniTask<TAsset> _GetLocalStoreOnlyAsync(AssetRequest<TKey> request) {
            Assert.IsNotNull(
                assetStore,
                $"[AssetProvider] assetStore is required. fetchMode={request.FetchMode}");

            var storeAsset = await _LoadFromStoreAsync(request.Key);
            if (!_IsValidAsset(request.Key, storeAsset))
                return default;

            _SaveCache(request.Key, storeAsset);
            return storeAsset;
        }
        #endregion

        #region Private - Source
        private async UniTask<TAsset> _GetSourceFirstAsync(AssetRequest<TKey> request) {
            var sourceAsset = await _LoadFromSourceAsync(request);
            if (_IsValidAsset(request.Key, sourceAsset)) {
                _SaveCache(request.Key, sourceAsset);
                await _SaveStoreAsync(request.Key, sourceAsset);
                return sourceAsset;
            }

            if (assetStore == null) return default;

            var storeAsset = await _LoadFromStoreAsync(request.Key);
            if (!_IsValidAsset(request.Key, storeAsset))  return default;

            _SaveCache(request.Key, storeAsset);
            return storeAsset;
        }

        private async UniTask<TAsset> _GetSourceOnlyAsync(AssetRequest<TKey> request) {
            var sourceAsset = await _LoadFromSourceAsync(request);
            if (!_IsValidAsset(request.Key, sourceAsset)) return default;

            _SaveCache(request.Key, sourceAsset);
            return sourceAsset;
        }
        #endregion

        #region Private - Load
        private async UniTask<TAsset> _LoadFromSourceAsync(AssetRequest<TKey> request) {
            var assetLoader = _ResolveLoader(request.LoadMode);
            var asset = await assetLoader.LoadAsync(request.Key);
            return asset;
        }

        private async UniTask<TAsset> _LoadFromStoreAsync(TKey key) {
            Assert.IsNotNull(assetStore, "[AssetProvider] assetStore is null.");
            if (!await assetStore.HasAsync(key)) return default;
            return await assetStore.LoadAsync(key);
        }
        #endregion

        #region Private - Save
        private void _SaveCache(TKey key, TAsset asset) {
            assetCache.Save(key, asset);
        }

        private UniTask _SaveStoreAsync(TKey key, TAsset asset) {
            if (assetStore == null) return UniTask.CompletedTask;
            return assetStore.SaveAsync(key, asset);
        }
        #endregion

        #region Private - Resolve
        private IAssetLoader<TKey, TAsset> _ResolveLoader(AssetLoadMode loadMode) {
            if (loaderTable.TryGetValue(loadMode, out var assetLoader)) {
                return assetLoader;
            }
            Assert.IsTrue(false, $"[AssetProvider] Loader not registered. loadMode={loadMode}");
            return default;
        }
        #endregion

        #region Private - Check
        private bool _IsValidAsset(TKey key, TAsset asset) {
            return assetChecker.IsValid(key, asset);
        }
        #endregion
    }
}
