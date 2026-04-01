using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine.Assertions;
using HUtil.AssetHandler.Cache;
using HUtil.AssetHandler.Data;
using HUtil.AssetHandler.Load;
using HUtil.AssetHandler.Store;
using HUtil.AssetHandler.Subscription;
using HUtil.AssetHandler.Validation;

#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * AssetHandler의 중심 진입점 provider 구현 스크립트입니다.
 *
 * 주의사항 ::
 * 1. cache, store, source 책임을 한곳에 직접 섞지 않고 조율만 해야 합니다.
 * 2. owner release와 source release는 각각 다른 경계를 통해 연결됩니다.
 * =========================================================
 */
#endif

namespace HUtil.AssetHandler.Provider {
    public sealed class AssetProvider<TKey, TAsset> : IAssetProvider<TKey, TAsset> {
        #region Fields
        readonly IAssetCache<TKey, TAsset> assetCache;
        readonly IAssetStore<TKey, TAsset> assetStore;
        readonly IAssetValidator<TKey, TAsset> assetValidator;
        readonly IAssetLoadGate<TKey, TAsset> assetLoadGate;
        readonly List<IAssetReleasableLoader<TKey, TAsset>> releasableLoaders = new();
        readonly Dictionary<AssetLoadMode, IAssetLoader<TKey, TAsset>> loaderTable = new();
        #endregion

        #region Public - Constructors
        public AssetProvider(
            IEnumerable<IAssetLoader<TKey, TAsset>> assetLoaders,
            IAssetCache<TKey, TAsset> assetCache,
            IAssetValidator<TKey, TAsset> assetValidator,
            IAssetLoadGate<TKey, TAsset> assetLoadGate,
            IAssetStore<TKey, TAsset> assetStore = null) {

            Assert.IsNotNull(assetLoaders, "[AssetProvider] loaders is null.");
            Assert.IsNotNull(assetCache, "[AssetProvider] cache is null.");
            Assert.IsNotNull(assetValidator, "[AssetProvider] validator is null.");
            Assert.IsNotNull(assetLoadGate, "[AssetProvider] loadGate is null.");

            this.assetCache = assetCache;
            this.assetValidator = assetValidator;
            this.assetLoadGate = assetLoadGate;
            this.assetStore = assetStore;
            this.assetCache.OnAssetRemoved += _OnAssetRemoved;

            foreach (var assetLoader in assetLoaders) {
                Assert.IsNotNull(assetLoader, "[AssetProvider] loader contains null.");
                loaderTable[assetLoader.LoadMode] = assetLoader;

                if (assetLoader is IAssetReleasableLoader<TKey, TAsset> releasableLoader) {
                    releasableLoaders.Add(releasableLoader);
                }
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
            AssetOwnerId ownerId = default) {

            var request = new AssetRequest<TKey>(
                key: key,
                loadMode: loadMode,
                fetchMode: fetchMode,
                ownerId: ownerId);

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

        public bool Release(TKey key, AssetOwnerId ownerId) {
            return assetCache.Release(key, ownerId);
        }

        public int ReleaseOwner(AssetOwnerId ownerId) {
            return assetCache.ReleaseOwner(ownerId);
        }

        public void ReleaseAll() {
            assetCache.ReleaseAll();
        }

        public void ClearCache() {
            assetCache.Clear();
        }

        public UniTask ClearStoreAsync() {
            if (assetStore == null) return UniTask.CompletedTask;
            return assetStore.ClearAsync();
        }
        #endregion

        #region Private - Get
        private async UniTask<TAsset> _GetAsync(AssetRequest<TKey> request) {
            if (!assetValidator.CanLoad(request.Key)) {
                return default;
            }

            var asset = await assetLoadGate.RunAsync(
                request.Key,
                () => _GetByFetchModeAsync(request));

            if (request.HasOwner && _IsValidAsset(request.Key, asset)) {
                assetCache.Save(request.Key, asset, request.OwnerId);
            }

            return asset;
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
            if (_TryLoadCache(request, out var cachedAsset)) {
                return cachedAsset;
            }

            var sourceAsset = await _LoadFromSourceAsync(request);
            if (!_IsValidAsset(request.Key, sourceAsset)) return default;

            _SaveCache(request, sourceAsset);
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
                _SaveCache(request, storeAsset);
                return storeAsset;
            }

            var sourceAsset = await _LoadFromSourceAsync(request);
            if (!_IsValidAsset(request.Key, sourceAsset)) return default;

            _SaveCache(request, sourceAsset);
            await _SaveStoreAsync(request.Key, sourceAsset);
            return sourceAsset;
        }

        private async UniTask<TAsset> _GetLocalStoreOnlyAsync(AssetRequest<TKey> request) {
            Assert.IsNotNull(
                assetStore,
                $"[AssetProvider] assetStore is required. fetchMode={request.FetchMode}");

            var storeAsset = await _LoadFromStoreAsync(request.Key);
            if (!_IsValidAsset(request.Key, storeAsset)) return default;

            _SaveCache(request, storeAsset);
            return storeAsset;
        }
        #endregion

        #region Private - Source
        private async UniTask<TAsset> _GetSourceFirstAsync(AssetRequest<TKey> request) {
            var sourceAsset = await _LoadFromSourceAsync(request);
            if (_IsValidAsset(request.Key, sourceAsset)) {
                _SaveCache(request, sourceAsset);
                await _SaveStoreAsync(request.Key, sourceAsset);
                return sourceAsset;
            }

            if (assetStore == null) return default;

            var storeAsset = await _LoadFromStoreAsync(request.Key);
            if (!_IsValidAsset(request.Key, storeAsset))  return default;

            _SaveCache(request, storeAsset);
            return storeAsset;
        }

        private async UniTask<TAsset> _GetSourceOnlyAsync(AssetRequest<TKey> request) {
            var sourceAsset = await _LoadFromSourceAsync(request);
            if (!_IsValidAsset(request.Key, sourceAsset)) return default;
            _SaveCache(request, sourceAsset);
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
        private bool _TryLoadCache(AssetRequest<TKey> request, out TAsset asset) {
            if (request.HasOwner) {
                return assetCache.TryLoad(request.Key, request.OwnerId, out asset);
            }
            return assetCache.TryLoad(request.Key, out asset);
        }

        private void _SaveCache(AssetRequest<TKey> request, TAsset asset) {
            if (request.HasOwner) {
                assetCache.Save(request.Key, asset, request.OwnerId);
                return;
            }
            assetCache.Save(request.Key, asset);
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

        #region Private - Release
        private bool _ReleaseAssetLoaders(TKey key) {
            bool released = false;

            foreach (var releasableLoader in releasableLoaders) {
                if (releasableLoader.Release(key)) {
                    released = true;
                }
            }

            return released;
        }
        #endregion

        #region Private - Event
        private void _OnAssetRemoved(TKey key, TAsset asset) {
            _ReleaseAssetLoaders(key);
        }
        #endregion

        #region Private - Validation
        private bool _IsValidAsset(TKey key, TAsset asset) {
            return assetValidator.IsValid(key, asset);
        }
        #endregion
    }
}

#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * 주요 기능 ::
 * 1. fetch mode에 따라 cache, store, source 순서를 조율합니다.
 * 2. load gate로 중복 로드를 합칩니다.
 * 3. owner 기반 release와 releasable loader release를 연결합니다.
 *
 * 사용법 ::
 * 1. 도메인 코드나 repository가 asset 조회 진입점으로 사용합니다.
 * 2. GetAsync와 ReleaseOwner를 통해 owner lifecycle과 연결합니다.
 * 3. loader 조합은 factory 또는 생성자로 주입합니다.
 *
 * 이벤트 ::
 * 1. cache에서 실제 제거가 일어나면 source release 경로가 이어질 수 있습니다.
 * 2. 직접 공개 이벤트는 없지만 하위 cache 이벤트를 구독합니다.
 *
 * 기타 ::
 * 1. AssetHandler 구조의 핵심 오케스트레이터입니다.
 * 2. source 세부 구현은 loader와 store에 위임합니다.
 * =========================================================
 */
#endif
