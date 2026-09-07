using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using HResource.Cache;
using HResource.Data;
using HResource.Load;
using HResource.Store;
using HResource.Subscription;
using HResource.Validation;
using HDiagnosis.Logger;
using UnityEngine;

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
 * owner 별 점유의 실제 보유자. 지문 발급은 AssetLeashManager 가 맡는다.
 *
 * 사용법 ::
 * AssetProviderFactory 로 조립하고 IAssetSource 경계로 쓴다. 소비자는 GetAsync(this, key, mode)
 * 하나만 알면 되고, 다 쓰면 Release(this, key) 를 부른다.
 *
 * 주의 ::
 * cache, store, source 책임을 한곳에 직접 섞지 않고 조율만 함. owner release 와 source release
 * 는 각각 다른 경계를 통해 연결 (cache.OnAssetRemoved → releasableLoader.Release).
 * 소유자를 생략한 획득 경로는 존재하지 않는다. IAssetSource 에 그런 멤버가 없다.
 *
 * 역할 경계 ::
 * - Provider (이 클래스) : cache/store/loader 조율 + owner 별 점유 소유.
 * - AssetLeashManager   : 지문 발급 + 파괴 프로브 부착 + 소유자 단위 회수.
 * - IAssetLeash         : 순수 C# 객체용 창구. using 으로 반납을 보증한다.
 *
 * 두 소유자 경로 ::
 * - Component  → GetAsync(this, ...). 파괴되면 프로브가 자동 회수한다.
 * - 순수 객체  → using var leash = source.Leash(this, anchor). anchor 파괴가 상한이다.
 * =========================================================
 */
#endif

namespace HResource.Provider {
    // IDisposable 은 IAssetSource 가 이미 상속한다 (2026-08-06).
    public sealed class AssetProvider<TKey, TAsset> : IAssetSource<TKey, TAsset> {
        #region Fields
        readonly IAssetCache<TKey, TAsset> assetCache;
        readonly IAssetStore<TKey, TAsset> assetStore;
        readonly IAssetValidator<TKey, TAsset> assetValidator;
        readonly IAssetLoadGate<TKey, TAsset> assetLoadGate;
        readonly AssetLeashManager<TKey, TAsset> leashManager;

        readonly Dictionary<AssetLoadMode, IAssetLoader<TKey, TAsset>> loaderTable = new();
        readonly Dictionary<TKey, IAssetReleasableLoader<TKey, TAsset>> releasableLoaderByKey = new();

        bool disposed;
        #endregion

        #region Public - Diagnostics
        // disposed 는 private 이라 "폐기됐는데 계속 쓰이는 중"을 테스트가 단정할 수 없었다
        // (케이스 리포트 07 TST-2). 진단 전용 읽기 프로퍼티만 노출한다. 상태를 바꾸지 않는다.
        public bool IsDisposed => disposed;
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

                if (loaderTable.ContainsKey(assetLoader.LoadMode)) {
                    HLogger.Warning(
                        $"[AssetProvider] Duplicate loader for LoadMode={assetLoader.LoadMode}. " +
                        "The previous loader is overwritten and becomes unreachable via _ResolveLoader.");
                }

                loaderTable[assetLoader.LoadMode] = assetLoader;
            }

            if (loaderTable.Count < 1) {
                HLogger.Throw(new ArgumentException(
                    "[AssetProvider] No asset loader registered.",
                    nameof(assetLoaders)));
            }

            // 이 provider 를 통과하는 모든 점유는 반드시 소유자를 갖는다.
            // 지문 발급과 파괴 감지는 leash manager 가 전담한다.
            leashManager = new AssetLeashManager<TKey, TAsset>(this);
        }
        #endregion

        #region Public - Get
        /// <summary> Component 소유자로 자산을 얻는다. 그 소유자가 파괴되면 이 점유는 자동 회수된다. </summary>
        public UniTask<TAsset> GetAsync(
            Component owner,
            TKey key,
            AssetLoadMode loadMode,
            AssetFetchMode fetchMode = AssetFetchMode.CacheFirst) {

            if (_RejectIfDisposed(nameof(GetAsync))) return UniTask.FromResult<TAsset>(default);

            var ownerId = leashManager.Fingerprint(owner);
            if (!ownerId.IsValid) return UniTask.FromResult<TAsset>(default);

            return GetForOwnerAsync(key, loadMode, fetchMode, ownerId);
        }

        /// <summary>
        /// 순수 C# 객체용 창구를 발급한다. using 으로 감싸는 것이 정상 플로우이고, 그것을 빠뜨려도 anchor 가 파괴되면 회수된다.
        /// </summary>
        public IAssetLeash<TKey, TAsset> Leash(object owner, Component anchor) {
            if (_RejectIfDisposed(nameof(Leash))) return null;
            return leashManager.Leash(owner, anchor);
        }

        // 지문이 확정된 뒤의 실제 획득 경로. leash 계층만 호출한다.
        internal UniTask<TAsset> GetForOwnerAsync(
            TKey key,
            AssetLoadMode loadMode,
            AssetFetchMode fetchMode,
            AssetOwnerId ownerId) {

            if (_RejectIfDisposed(nameof(GetForOwnerAsync))) return UniTask.FromResult<TAsset>(default);

            var request = new AssetRequest<TKey>(
                key: key,
                loadMode: loadMode,
                fetchMode: fetchMode,
                ownerId: ownerId);

            return _GetAsync(request);
        }

        public bool TryGet(TKey key, out TAsset asset) {
            if (_RejectIfDisposed(nameof(TryGet))) {
                asset = default;
                return false;
            }

            return assetCache.TryGet(key, out asset);
        }
        #endregion

        #region Public - Release
        /// <summary> 정상 반납 경로. 그 자산을 다 쓴 시점에 부른다. </summary>
        public bool Release(Component owner, TKey key) {
            if (_RejectIfDisposed(nameof(Release))) return false;

            // 점유한 적이 없는 소유자면 발급하지 않는다. 빈 지문을 늘리지 않기 위해서다.
            if (!leashManager.TryFingerprint(owner, out var ownerId)) return false;
            return assetCache.Release(key, ownerId);
        }

        /// <summary> 이 소유자의 점유를 일괄 반납한다. 파괴를 기다리지 않고 지금 놓는다. </summary>
        public int ReleaseOwner(Component owner) {
            if (_RejectIfDisposed(nameof(ReleaseOwner))) return 0;
            if (owner == null) return 0;
            return leashManager.Reclaim(owner);
        }

        // 지문이 확정된 뒤의 실제 해제 경로. leash 계층만 호출.
        internal bool ReleaseForOwner(TKey key, AssetOwnerId ownerId) {
            if (_RejectIfDisposed(nameof(ReleaseForOwner))) return false;
            return assetCache.Release(key, ownerId);
        }

        internal int ReleaseOwnerId(AssetOwnerId ownerId) {
            if (_RejectIfDisposed(nameof(ReleaseOwnerId))) return 0;
            return assetCache.ReleaseOwner(ownerId);
        }

        public void ClearCache() {
            if (_RejectIfDisposed(nameof(ClearCache))) return;
            assetCache.Clear();
        }

        /// <summary> 소유자를 잃은 점유의 수동 일괄 회수. 판정은 leash 계층의 약한 표 </summary>
        public int ReclaimOrphans() {
            if (_RejectIfDisposed(nameof(ReclaimOrphans))) return 0;
            return leashManager.ReclaimDeadOwners();
        }

        /// <summary>
        /// provider 폐기. 남은 점유를 전부 해제해 loader release 연쇄를 태운 뒤 cache 이벤트 구독을 끊는다.
        /// 소유자(MonoBehaviour 등)의 OnDestroy 에서 호출한다. 두 번 호출해도 안전하다.
        /// 폐기 이후의 모든 공개 API 호출은 경고와 함께 거부된다.
        /// </summary>
        public void Dispose() {
            if (disposed) return;

            // 순서 주의 1 : disposed 를 ReleaseAll 보다 먼저 실행. 알림 구독자가 재진입해 Dispose 를 재호출하면 정지.
            // 순서 주의 2 : assetCache 를 직접 호출은 ReleaseAll() 가 disposed 를 보고 거부하기 때문. 폐기의 마지막 정리는 가드를 우회.
            disposed = true;

            // 순서 주의 3 : 구독을 먼저 끊으면 OnAssetRemoved 가 오지 않아 loader 핸들이 그대로 남는다. 반드시 ReleaseAll 로 연쇄를 태운 뒤 구독을 해제.
            assetCache.ReleaseAll();
            assetCache.OnAssetRemoved -= _OnAssetRemoved;

            // leash 계층도 닫는다. 살아남은 프로브 콜백은 여기서 무력화.
            leashManager.Dispose();
        }

        public UniTask ClearStoreAsync() {
            if (_RejectIfDisposed(nameof(ClearStoreAsync))) return UniTask.CompletedTask;
            if (assetStore == null) return UniTask.CompletedTask;
            return assetStore.ClearAsync();
        }
        #endregion

        #region Private - Disposed Guard
        /// <summary> 폐기된 provider 로 들어온 요청이면 경고를 남기고 true 를 반환한다. </summary>
        // cache 의 OnAssetRemoved 구독 해제.
        // 그 뒤로도 조회·해제가 동작하면 "캐시에는 등록되는데 로더 핸들은 아무도 회수하지 않는" 상태 발생.
        private bool _RejectIfDisposed(string apiName) {
            if (!disposed) return false;

            HLogger.Warning(
                $"[AssetProvider] {apiName} called after Dispose. The request is rejected - check the owner's lifetime.");
            return true;
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

            // await 는 프레임을 넘긴다. 그 사이 소유자가 파괴되어 Dispose 가 돌았을 수 있다.
            if (disposed) {
                HLogger.Warning(
                    $"[AssetProvider] Disposed while loading key '{request.Key}'. Releasing the in-flight loader handle.");
                _ReleaseLoaderHandle(request.LoadMode, request.Key);
                return default;
            }

            // 점유 등록은 게이트 밖에서 호출자마다 수행.
            // 게이트 안(factory)은 최초 호출자 1회만 실행,
            // 내부에서 등록하면 중복 이슈 발생. 후속 호출자들 미등록 상태로 asset 받아 다른 호출자 Release 한 번에 조기 해제.
            if (_IsValidAsset(request.Key, asset)) {
                // Save 가 거부되면(동일 키 에셋 이미 등록) 호출자는 등록되지 않은 asset 을 받고,
                // 그 asset 의 로더 핸들은 누구도 해제하지 않는 누수.
                if (!_SaveCache(request, asset)) {
                    HLogger.Error(
                        $"[AssetProvider] Cache rejected key '{request.Key}'. Releasing the freshly loaded asset to avoid a handle leak.");
                    _ReleaseLoaderHandle(request.LoadMode, request.Key);
                    return default;
                }

                // 캐시에 실제로 올라간 asset 을 로드한 loader 만 기록.
                // 이후 이 key 가 OnAssetRemoved 로 제거될 때 이 loader 하나만 release 되도록.
                _TrackReleasableLoader(request.Key, request.LoadMode);

                // ============================================================
                // 위치 주의 : 반드시 _TrackReleasableLoader 뒤에 둔다.
                // 죽은 소유자가 유일한 보유자였다면 아래 Release 가 _TryRemoveItem -> OnAssetRemoved 연쇄를 태운다.
                // 
                // 그 연쇄의 마지막이 releasableLoaderByKey 를 조회.
                // 이 검사를 추적보다 앞에 두면 조회가 비어 Addressable 핸들이 그대로 잔존.
                //
                // await 사이에 소유자가 사라졌으면 방금 만든 점유를 즉시 되돌린다.
                // 소유자가 죽으면 프로브가 이미 회수를 마쳤으므로, 여기서 등록된 점유는
                // 아무도 내려놓을 수 없는 ORPHAN 이 된다 (케이스 리포트 RACE-1).
                //
                // 위의 disposed 가드처럼 핸들을 직접 반납하지 않는 이유는 '중복' 때문이다.
                // provider 폐기는 모든 대기자가 함께 빠지지만, 소유자별 판정은 "A 는 죽고 B 는 살아있는" 혼합 상황이 발생.
                //
                // SharedGate 가 같은 key 요청을 하나로 합치므로 핸들을 직접 반납하면 살아있는 B 의 자산까지 무효화된다.
                // 정상 해제 경로를 태우면 마지막 점유일 때만 OnAssetRemoved 로 반납된다.
                // ============================================================
                if(!leashManager.IsLive(request.OwnerId)) {
                    HLogger.Warning(
                        $"[AssetProvider] The owner died while loading key '{request.Key}'." +
                        " Releasing the occupancy it would have taken.");
                    assetCache.Release(request.Key, request.OwnerId);
                    return default;
                }
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
            if (_TryPeekCache(request, out var cachedAsset)) {
                return cachedAsset;
            }

            var sourceAsset = await _LoadFromSourceAsync(request);
            if (!_IsValidAsset(request.Key, sourceAsset)) return default;

            await _SaveStoreOrReleaseSourceAsync(request, sourceAsset);
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
                return storeAsset;
            }

            var sourceAsset = await _LoadFromSourceAsync(request);
            if (!_IsValidAsset(request.Key, sourceAsset)) return default;

            await _SaveStoreOrReleaseSourceAsync(request, sourceAsset);
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

            return storeAsset;
        }
        #endregion

        #region Private - Source
        private async UniTask<TAsset> _GetSourceFirstAsync(AssetRequest<TKey> request) {
            var sourceAsset = await _LoadFromSourceAsync(request);
            if (_IsValidAsset(request.Key, sourceAsset)) {
                await _SaveStoreOrReleaseSourceAsync(request, sourceAsset);
                return sourceAsset;
            }

            if (assetStore == null) return default;

            var storeAsset = await _LoadFromStoreAsync(request.Key);
            if (!_IsValidAsset(request.Key, storeAsset)) return default;

            return storeAsset;
        }

        private async UniTask<TAsset> _GetSourceOnlyAsync(AssetRequest<TKey> request) {
            var sourceAsset = await _LoadFromSourceAsync(request);
            if (!_IsValidAsset(request.Key, sourceAsset)) return default;
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
        private bool _TryPeekCache(AssetRequest<TKey> request, out TAsset asset) {
            // 점유 등록 없이 존재만 확인 - 등록은 _GetAsync 의 게이트 밖 _SaveCache 가 담당.
            return assetCache.TryGet(request.Key, out asset);
        }

        private bool _SaveCache(AssetRequest<TKey> request, TAsset asset) {
            // 진입점이 leash 지문을 확정하므로 여기 도달하는 요청은 항상 유효한 owner 소유.
            return assetCache.Save(request.Key, asset, request.OwnerId);
        }


        // 로더가 이미 핸들을 잡은 뒤의 단계다. 여기서 예외가 나가면 _GetAsync 의 _SaveCache 에
        // 도달하지 못해 "캐시 등록은 없는데 로더 핸들만 살아있는" 불일치가 남고, 그 핸들은
        // OnAssetRemoved 연쇄가 돌지 않으므로 아무도 해제하지 않는다.
        // 예외는 삼키지 않고 그대로 올리되, 나가기 전에 방금 잡은 핸들만 되돌려 놓는다.
        private async UniTask _SaveStoreOrReleaseSourceAsync(AssetRequest<TKey> request, TAsset asset) {
            if (assetStore == null) return;

            try {
                await assetStore.SaveAsync(request.Key, asset);
            }
            catch {
                HLogger.Error(
                    $"[AssetProvider] Store save failed after the source load for key '{request.Key}'. Releasing the loader handle before rethrowing.");
                _ReleaseLoaderHandle(request.LoadMode, request.Key);
                throw;
            }
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
        // 캐시 등록 이전(로드 직후 / store 저장 실패) 단계의 해제. 이 요청이 실제로 사용한
        // loadMode 의 loader 하나만 건드린다 - 다른 loadMode 의 loader 가 같은 key 로 이미
        // 캐시에 올려둔 살아있는 핸들을 대신 회수하는 사고를 막는다.
        private bool _ReleaseLoaderHandle(AssetLoadMode loadMode, TKey key) {
            if (_ResolveLoader(loadMode) is IAssetReleasableLoader<TKey, TAsset> releasableLoader) {
                return releasableLoader.Release(key);
            }
            return false;
        }

        // 캐시에 실제로 등록된 key 가 어떤 loader 로 로드됐는지 기록한다. Save 성공 직후에만
        // 호출 - 실패한(캐시가 거부한) 로드는 _ReleaseLoaderHandle 로 직접 처리하므로 기록하지 않는다.
        private void _TrackReleasableLoader(TKey key, AssetLoadMode loadMode) {
            if (_ResolveLoader(loadMode) is IAssetReleasableLoader<TKey, TAsset> releasableLoader) {
                releasableLoaderByKey[key] = releasableLoader;
            }
        }

        // 캐시가 key 를 완전히 제거했을 때(OnAssetRemoved) 그 key 를 실제로 로드했던 loader
        // 하나만 release 한다. 등록되지 않은 key(비-releasable loader 로만 로드된 경우 등)는
        // 무해하게 false 를 반환한다.
        private bool _ReleaseTrackedLoader(TKey key) {
            if (!releasableLoaderByKey.TryGetValue(key, out var releasableLoader)) return false;
            releasableLoaderByKey.Remove(key);
            return releasableLoader.Release(key);
        }
        #endregion
        #region Private - Event
        private void _OnAssetRemoved(TKey key, TAsset asset) {
            _ReleaseTrackedLoader(key);
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
 * 2026-09-07 (수정) :: ReclaimOrphans 판정을 leash 계층으로 옮김
 * =========================================================
 * 변경 ::
 * 캐시의 소유자 목록을 훑던 본문을 버리고 leashManager.ReclaimDeadOwners 에 위임한다.
 *
 * 이유 ::
 * 캐시는 소유자 생존을 모른다. 캐시 쪽 판정은 "엔트리가 없는 신원" 만 잡는데,
 * 실제 orphan 은 "엔트리는 있고 소유자가 죽은" 쪽이라 대상을 통째로 비껴갔다.
 *
 * 결과 ::
 * 회수가 _ReclaimEntry 단일 창구를 탄다.
 * 어제 항목이 적어 둔 "GC 된 순수 소유자는 걸리지 않는다" 는 한계는 여기서 해소됐다.
 *
 * 주의 ::
 * 재진입 대비와 순회 중 변형 방지는 이제 leash 계층이 진다.
 *
 * =========================================================
 * 2026-09-06 (수정) :: ReclaimOrphans 구현
 * =========================================================
 * 변경 ::
 * 캐시의 소유자 목록을 떠 IsLive 가 false 인 신원을 leash 계층에 넘겨 회수한다.
 *
 * 이유 ::
 * 정상 경로에서는 창구가 사라질 때 점유도 사라진다. 둘이 어긋난 상태는 아무도 회수하지 못한다.
 *
 * 결과 ::
 * 수동 호출 한 번으로 그 이상 상태가 정리된다. 반환값은 회수한 key 수다.
 *
 * 주의 ::
 * 버퍼를 먼저 뜬 뒤 순회한다. 회수가 캐시의 소유자 테이블을 바꾸기 때문이다.
 * Dispose 없이 GC 된 순수 소유자는 liveEntries 에 남아 이 판정에 걸리지 않는다.
 *
 * =========================================================
 * 2026-09-04 (수정) :: 소유자 강제를 타입으로 올리고 지문 관리를 분리
 * 
 * 변경 ::
 * - IAssetProvider 를 폐기하고 IAssetSource 를 구현한다. 자산 접근 멤버가 전부 소유자를 요구한다.
 * - selfOwnerId 제거. 매니저도 특례 없이 소유자 하나로 참여한다.
 * - AssetLeashManager 를 상주 객체로 들고 지문 발급과 파괴 감지를 위임한다.
 * - ownerId 를 직접 받던 오버로드는 GetForOwnerAsync / ReleaseForOwner / ReleaseOwnerId 로
 *   이름을 바꾸고 internal 로 강등했다. leash 계층만 호출한다.
 * - _ResolveOwner / _WithResolvedOwner 제거. 무효 owner 를 치환할 일이 없어졌다.
 *
 * 이유 ::
 * AssetOwnerId 는 struct 라 default 가 항상 존재해 매개변수로는 강제가 불가능하다.
 * 직전 작업의 selfOwnerId 는 추적 불가를 추적 가능으로 낮춘 타협이었을 뿐,
 * 소유자를 실제 사용처에 두지는 못했다. provider 1개 / 매니저 1개 / 사용 객체 N개 라는
 * 실제 구조에서는 N 쪽 각자가 소유자여야 한다.
 *
 * 결과 ::
 * source.GetAsync(key, mode) 는 컴파일되지 않는다. 소비자는 인터페이스 구현도 창구 보관도
 * 필요 없고 GetAsync(this, ...) 만 안다. 반납을 잊고 파괴되면 프로브가 회수한다.
 *
 * 주의 ::
 * 자동 회수는 GameObject 파괴에 한한다. Destroy(component) 단독과 순수 C# 객체는 잡히지 않는다.
 * 정상 플로우는 여전히 명시적 Release 다. 프로브는 안전망이지 대체재가 아니다.
 * =========================================================
 * @Jason - PKH 2026.09.04 소유자 없는 획득 제거 (케이스 리포트 EDGE-1)
 *
 * # 변경
 * - selfOwnerId 신설. 생성자에서 AssetOwnerIdGenerator.NewId(this) 로 발급한다.
 * - GetAsync 두 진입점이 소유자를 생략한 요청을 selfOwnerId 로 귀속시킨다.
 * - Release(key) 무인자 오버로드도 같은 id 로 해제한다. 획득과 해제의 짝이 보존된다.
 * - _SaveCache 의 익명 분기를 제거했다. 진입점에서 해석하므로 도달할 수 없다.
 * - Dispose 에서 selfOwnerId 를 NotifyReleased 한다.
 *
 * # 이유
 * - 종전에는 ownerId 기본값이 무효값이라 소유자 없는 점유가 만들어졌다.
 *   그 점유는 Owner Watcher 에 나타날 수 없고 ReleaseOwner 로도 회수되지 않아,
 *   누수 추적에서 원리적으로 보이지 않는 구멍이었다.
 * - 이제 이 provider 를 통과하는 모든 점유가 소유자를 갖는다. 최소 단위는 provider 자신이다.
 *
 * # 결과
 * - 공개 시그니처는 그대로다. 호출자 코드 변경이 필요 없다.
 *   HResource / HResource.Editor / HAudio 를 Roslyn 으로 빌드해 에러 0 을 확인했다.
 * - MemoryAssetCache 의 AnonymousDependency 축은 provider 경로에서 도달 불가가 된다.
 *   캐시를 직접 쓰는 코드를 위해 남겨 두었고, 제거는 별도 판단이다.
 *
 * # 주의
 * - 소유자를 명시하지 않는 호출은 이제 provider 수명 동안 점유가 유지된다.
 *   ReleaseAll / Dispose 없이 provider 를 버리면 그대로 누수다 (케이스 리포트 USR-4).
 *
 * =========================================================
 * 2026-08-07 (수정 4) :: IsDisposed 진단 프로퍼티 추가 (케이스 리포트 07 TST-2)
 * 
 * 변경 ::
 * `disposed` 필드를 읽기 전용으로 노출하는 `public bool IsDisposed` 프로퍼티 신설.
 *
 * 이유 ::
 * `disposed` 가 private 이라 "폐기 후에도 계속 쓰이는 중"을 테스트가 단정할 방법이 없었다.
 * 리포트가 제안한 최소 처방(진단 프로퍼티)만 적용 - 상태를 바꾸지 않는 읽기 전용이라
 * NEG-1/RACE-1 가드 로직에는 영향이 없다. 인터페이스(IAssetProvider)에는 올리지 않았다 -
 * 구현체가 1개뿐이고 테스트는 구체 타입으로 직접 생성하므로 계약 확장까지는 불필요하다.
 *
 * =========================================================
 * 2026-08-06 (수정 3) :: key 단위 releasable loader 추적 + 중복 LoadMode 경고 (감사 5차 HResource 항목 5·9)
 * 
 * 변경 ::
 * 1) releasableLoaders(List, 전체 releasable loader) 를 releasableLoaderByKey(Dictionary,
 *    key → 실제로 그 key 를 로드한 loader 1개) 로 교체. Save 성공 직후에만 등록(_TrackReleasableLoader).
 * 2) 캐시 등록 이전 단계(로드 직후 Save 거부 / store 저장 실패 / 로딩 중 Dispose)의 해제는
 *    request.LoadMode 로 해석한 그 loader 하나만(_ReleaseLoaderHandle), 캐시 제거(OnAssetRemoved)
 *    시점의 해제는 추적된 loader 하나만(_ReleaseTrackedLoader) 건드리도록 분리.
 * 3) 생성자에서 동일 LoadMode 로더가 중복 등록되면 loaderTable 덮어쓰기 전에 경고 로그 추가.
 *
 * 이유 ::
 * 1) 기존 _ReleaseAssetLoaders 는 등록된 모든 releasable loader 에 대해 Release(key) 를 호출했다.
 *    여러 loader 가 등록된 provider 에서 같은 key 문자열을 다른 LoadMode 로 이미 캐시에 올려
 *    살아있는 다른 loader 의 핸들까지 도매금으로 회수해 버릴 수 있었다 - 특히 Save 거부 롤백
 *    경로(같은 key 에 다른 asset 이 이미 캐시됨)에서 이 사고가 정확히 그 상황이다.
 *    현재 실사용(AssetProviderFactory)은 loader 1개뿐이라 미도달이지만, 생성자가 다중 loader
 *    조립을 허용하는 이상 잠재 결함으로 남겨두지 않는다.
 * 2) 동일 LoadMode 중복 등록은 이전 loader 를 조용히 덮어써 그 loader 로 로드된 자산의 release
 *    경로(_ResolveLoader 경유)가 통째로 사라진다. 등록 자체를 막지 않는 이유는 기존 계약(예외
 *    없이 마지막 등록이 승리)을 유지하면서 관측 가능성만 확보하기 위함 - 다른 Medium 처방(NEG-2)과
 *    동일한 판단.
 *
 * =========================================================
 * 2026-08-06 (수정 2) :: 폐기 후 진입 가드 (케이스 리포트 07 NEG-1 / RACE-1 / USR-3)
 * 
 * 변경 ::
 * 1) 공개 API 9종(GetAsync 2 / TryGet / Release 2 / ReleaseOwner / ReleaseAll / ClearCache /
 *    ClearStoreAsync) 진입부에 _RejectIfDisposed 가드 추가 - 경고 후 무해값 반환.
 * 2) _GetAsync 의 await 재개 직후 disposed 재검사 - in-flight 로 잡은 로더 핸들 회수 후 종료.
 * 3) Dispose 내부는 공개 API 대신 assetCache 를 직접 호출 (자기 가드에 걸리지 않도록).
 *
 * 이유 ::
 * Dispose 가 cache 구독을 끊은 뒤에도 조회·해제가 동작해서, "캐시에는 등록되는데 로더 핸들은
 * 아무도 회수하지 않는" 상태를 만들 수 있었다. 진입 가드만으로는 "요청은 폐기 전, 완료는 폐기
 * 후" 인 비동기 건이 남으므로 재개 시점 재검사를 함께 넣었다. 이 둘로 리포트 07 의 NEG-1 ·
 * RACE-1 · USR-3(자식이 폐기된 provider 를 계속 참조) 세 건이 동시에 닫힌다.
 * 조용히 통과시키지 않는 이유 : 폐기 후 진입은 전부 호출자 측 수명 관리 오류라 드러나야 한다.
 *
 * =========================================================
 * 2026-08-06 (수정) :: Dispose 실효화 + store 저장 실패 시 로더 핸들 회수 (케이스 리포트 01 TST-2/EXC-2)
 * 
 * 변경 ::
 * 1) Dispose 가 구독 해제만 하던 것을 ReleaseAll → 구독 해제 순서로 확장 + 재진입 가드.
 *    IAssetProvider 가 IDisposable 을 상속하도록 바꿔 인터페이스 타입 필드에서도 호출 가능.
 * 2) _SaveStoreAsync 를 _SaveStoreOrReleaseSourceAsync 로 교체 - 저장 실패 시 로더 핸들
 *    회수 후 예외 재던짐. 호출 3지점(CacheFirst / LocalStoreFirst / SourceFirst) 동시 적용.
 *
 * 이유 ::
 * 1) 호출자 0건의 원인은 "부르는 사람이 없다" 가 아니라 소유자들이 전부 IAssetProvider 타입
 *    필드로 들고 있어 IDisposable 이 보이지 않았던 것이다. 계약에 올려 도달 가능하게 했다.
 *    ReleaseAll 을 먼저 태우지 않으면 구독이 끊긴 뒤 남은 점유의 loader 핸들이 영구 잔존한다.
 * 2) 로더가 핸들을 잡은 직후의 await 에서 예외가 나면 _SaveCache 에 도달하지 못해 캐시 등록
 *    없이 핸들만 남는다. OnAssetRemoved 연쇄 대상이 아니므로 아무도 해제하지 않는다.
 *    예외 자체는 삼키지 않는다 (전역 CLAUDE.md 무음 실패 금지) - 핸들만 되돌리고 재던진다.
 *
 * =========================================================
 * 2026-04-26 (수정) :: 헤더 형틀 통합 + Dev Log 형식 도입
 * 
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
 * 
 * 5 컴포넌트 (Cache / Store / Loader[] / Validator / LoadGate) 를 생성자 주입받아 조율만 하는
 * Composite Root + Strategy 오케스트레이터. 각 컴포넌트는 인터페이스로 교체 가능 - Strategy.
 * loader 는 List 한 개 (모든 loader) + List 한 개 (releasable 만) 두 컬렉션으로 분리하여
 * release 연쇄 시 release 가능한 것들만 순회 (성능 + 의도 표현 동시 달성).
 *
 * 5 가지 fetch mode 분기는 _GetByFetchModeAsync switch 한 곳에 집중 - 정책 추가 시 enum +
 * switch 한 줄 동시 갱신. cache → loader release 연쇄는 cache.OnAssetRemoved 이벤트 한 줄
 * 구독으로 묶임 (Cache 와 Loader 의 결합도 0).
 *
 * 생성자에서 모든 컴포넌트 null 검사 + HLogger.Throw - fail-fast. 이후 동작은 모든 컴포넌트
 * 가 살아있다는 정의상 보장. sealed 키워드로 상속 차단 (오케스트레이터 책임 침범 방지).
 * =========================================================
 */
#endif
