using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using HUtil.AssetHandler.Cache;
using HUtil.AssetHandler.Data;
using HUtil.AssetHandler.Load;
using HUtil.AssetHandler.Store;
using HUtil.AssetHandler.Subscription;
using HUtil.AssetHandler.Validation;
using HDiagnosis.Logger;

#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * AssetHandler 의 중심 진입점 provider 구현. 5 컴포넌트 (Cache / Store / Loader[] / Validator
 * / LoadGate) 의 단일 오케스트레이터.
 *
 * 주요 기능 ::
 * 5 가지 fetch mode (CacheFirst / LocalStoreFirst / LocalStoreOnly / SourceFirst / SourceOnly)
 * 를 _GetByFetchModeAsync switch 한 곳에 모아 cache/store/source 호출 순서 조율.
 * SharedAssetLoadGate 로 동일 key 동시 요청 dedupe.
 * cache 제거 시 OnAssetRemoved → releasable loader 자동 release 연쇄.
 * owner-aware reference counting 의 실제 보유자 (Subscription/IAssetLease 는 표현 계층).
 *
 * 사용법 ::
 * 도메인 코드나 repository 가 IAssetProvider 경계로 자산 조회. AssetProviderFactory 로 빠른
 * 조립 또는 생성자 직접 호출로 컴포넌트 커스텀. owner lifecycle 짝맞춤은 ReleaseOwner.
 *
 * 주의 ::
 * cache, store, source 책임을 한곳에 직접 섞지 않고 조율만 함. owner release 와 source release
 * 는 각각 다른 경계를 통해 연결 (cache.OnAssetRemoved → releasableLoader.Release).
 *
 * 역할 경계 ::
 * - Provider (이 클래스) : cache/store/loader 조율 + owner 기반 reference counting 소유. 실 보유자.
 * - AssetLeaseManager   : provider.GetAsync + Release 짝맞춤을 IDisposable 로 표현하는 보조 계층.
 * - IAssetLease         : 단일 key 한 점의 수명 핸들. Dispose 시 provider.Release(key, ownerId) 호출.
 *
 * 직접 사용 vs 래핑 사용 기준 ::
 * - 오너 수명 단순 + 한두 건의 수동 Release 로 충분 → provider.GetAsync + Release 직접 호출.
 * - 오너가 다수 자산 보유 + Dispose 짝을 실수 없이 보장 → AssetLeaseManager 얹어 사용.
 * - 오너 파괴 시 전체 일괄 회수 → ReleaseOwner(ownerId) 는 provider 에서만 호출 가능.
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

            if (assetLoaders == null) HLogger.Throw(new ArgumentNullException(nameof(assetLoaders)));
            if (assetCache == null) HLogger.Throw(new ArgumentNullException(nameof(assetCache)));
            if (assetValidator == null) HLogger.Throw(new ArgumentNullException(nameof(assetValidator)));
            if (assetLoadGate == null) HLogger.Throw(new ArgumentNullException(nameof(assetLoadGate)));

            this.assetCache = assetCache;
            this.assetValidator = assetValidator;
            this.assetLoadGate = assetLoadGate;
            this.assetStore = assetStore;
            this.assetCache.OnAssetRemoved += _OnAssetRemoved;

            foreach (var assetLoader in assetLoaders) {
                if (assetLoader == null) {
                    HLogger.Throw(new ArgumentException(
                        "[AssetProvider] asset loader collection contains null.",
                        nameof(assetLoaders)));
                }

                loaderTable[assetLoader.LoadMode] = assetLoader;

                if (assetLoader is IAssetReleasableLoader<TKey, TAsset> releasableLoader) {
                    releasableLoaders.Add(releasableLoader);
                }
            }

            if (loaderTable.Count < 1) {
                HLogger.Throw(new ArgumentException(
                    "[AssetProvider] No asset loader registered.",
                    nameof(assetLoaders)));
            }
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
                HLogger.Throw(
                        new NotSupportedException(),
                        $"[AssetProvider] Unsupported fetchMode. fetchMode={request.FetchMode}"
                    );
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
            if (assetStore == null) {
                HLogger.Throw(new InvalidOperationException(
                    $"[AssetProvider] assetStore is required. fetchMode={request.FetchMode}"));
                return default;
            }

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
            if (assetStore == null) {
                HLogger.Throw(new InvalidOperationException(
                    $"[AssetProvider] assetStore is required. fetchMode={request.FetchMode}"));
                return default;
            }

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
            return await assetLoader.LoadAsync(request.Key);
        }

        private async UniTask<TAsset> _LoadFromStoreAsync(TKey key) {
            if (assetStore == null) {
                HLogger.Throw(new InvalidOperationException("[AssetProvider] assetStore is null."));
                return default;
            }

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

            HLogger.Throw(new InvalidOperationException(
                $"[AssetProvider] Loader not registered. loadMode={loadMode}"));
            return null;
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
 * Dev Log
 * =========================================================
 *
 * =========================================================
 * 2026-04-26 (수정) :: 헤더 형틀 통합 + Dev Log 형식 도입
 * =========================================================
 * 변경 ::
 * 기존 헤더 (상단 도입+주의사항 + 하단 주요기능/사용법/이벤트/기타 + 역할 경계 + 직접/래핑
 * 사용 기준 등 다중 섹션) 를 한 곳에 통합하여 §11 형틀 통일. 하단 Dev Log 영역 추가.
 * 헤더와 Dev Log 모두 #if UNITY_EDITOR 가드.
 *
 * 이유 ::
 * 글로벌 CLAUDE.md §11 룰 일괄 적용. AssetProvider 가 시스템의 핵심 진입점이라 역할 경계와
 * 사용 기준을 헤더에 두어 reader 가 파일 진입 즉시 시스템 전체를 조망할 수 있도록.
 *
 * =========================================================
 * 2026-04-25 (최초 설계) :: AssetProvider 초기 구현
 * =========================================================
 * 5 컴포넌트 (Cache / Store / Loader[] / Validator / LoadGate) 를 생성자 주입받아 조율만 하는
 * Composite Root + Strategy 오케스트레이터. 각 컴포넌트는 인터페이스로 교체 가능 — Strategy.
 * loader 는 List 한 개 (모든 loader) + List 한 개 (releasable 만) 두 컬렉션으로 분리하여
 * release 연쇄 시 release 가능한 것들만 순회 (성능 + 의도 표현 동시 달성).
 *
 * 5 가지 fetch mode 분기는 _GetByFetchModeAsync switch 한 곳에 집중 — 정책 추가 시 enum +
 * switch 한 줄 동시 갱신. cache → loader release 연쇄는 cache.OnAssetRemoved 이벤트 한 줄
 * 구독으로 묶임 (Cache 와 Loader 의 결합도 0).
 *
 * 생성자에서 모든 컴포넌트 null 검사 + HLogger.Throw — fail-fast. 이후 동작은 모든 컴포넌트
 * 가 살아있다는 정의상 보장. sealed 키워드로 상속 차단 (오케스트레이터 책임 침범 방지).
 * =========================================================
 */
#endif
