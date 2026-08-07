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

namespace HResource.Provider {
    // IDisposable 은 IAssetProvider 가 이미 상속한다 (2026-08-06).
    public sealed class AssetProvider<TKey, TAsset> : IAssetProvider<TKey, TAsset> {
        #region Fields
        readonly IAssetCache<TKey, TAsset> assetCache;
        readonly IAssetStore<TKey, TAsset> assetStore;
        readonly IAssetValidator<TKey, TAsset> assetValidator;
        readonly IAssetLoadGate<TKey, TAsset> assetLoadGate;
        readonly List<IAssetReleasableLoader<TKey, TAsset>> releasableLoaders = new();
        readonly Dictionary<AssetLoadMode, IAssetLoader<TKey, TAsset>> loaderTable = new();
        bool disposed;
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
            if (_RejectIfDisposed(nameof(GetAsync))) return UniTask.FromResult<TAsset>(default);
            return _GetAsync(request);
        }

        public UniTask<TAsset> GetAsync(
            TKey key,
            AssetLoadMode loadMode,
            AssetFetchMode fetchMode = AssetFetchMode.CacheFirst,
            AssetOwnerId ownerId = default) {

            if (_RejectIfDisposed(nameof(GetAsync))) return UniTask.FromResult<TAsset>(default);

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
        public bool Release(TKey key) {
            if (_RejectIfDisposed(nameof(Release))) return false;
            return assetCache.Release(key);
        }

        public bool Release(TKey key, AssetOwnerId ownerId) {
            if (_RejectIfDisposed(nameof(Release))) return false;
            return assetCache.Release(key, ownerId);
        }

        public int ReleaseOwner(AssetOwnerId ownerId) {
            if (_RejectIfDisposed(nameof(ReleaseOwner))) return 0;
            return assetCache.ReleaseOwner(ownerId);
        }

        public void ReleaseAll() {
            if (_RejectIfDisposed(nameof(ReleaseAll))) return;
            assetCache.ReleaseAll();
        }

        public void ClearCache() {
            if (_RejectIfDisposed(nameof(ClearCache))) return;
            assetCache.Clear();
        }

        /// <summary>
        /// provider 폐기. 남은 점유를 전부 해제해 loader release 연쇄를 태운 뒤 cache 이벤트 구독을 끊는다.
        /// 소유자(MonoBehaviour 등)의 OnDestroy 에서 호출한다. 두 번 호출해도 안전하다.
        /// 폐기 이후의 모든 공개 API 호출은 경고와 함께 거부된다.
        /// </summary>
        public void Dispose() {
            if (disposed) return;

            // 순서 주의 1 : disposed 를 ReleaseAll 보다 먼저 세운다. 알림 구독자가 재진입해
            // Dispose 를 다시 부르면 여기서 끊긴다.
            // 순서 주의 2 : 아래에서 assetCache 를 직접 부르는 이유는 ReleaseAll() 공개 API 가
            // 이제 disposed 를 보고 거부하기 때문이다. 폐기의 마지막 정리는 가드를 우회한다.
            disposed = true;

            // 순서 주의 3 : 구독을 먼저 끊으면 OnAssetRemoved 가 오지 않아 loader 핸들이 그대로 남는다.
            // 반드시 ReleaseAll 로 연쇄를 태운 뒤 구독을 해제한다.
            assetCache.ReleaseAll();
            assetCache.OnAssetRemoved -= _OnAssetRemoved;
        }

        public UniTask ClearStoreAsync() {
            if (_RejectIfDisposed(nameof(ClearStoreAsync))) return UniTask.CompletedTask;
            if (assetStore == null) return UniTask.CompletedTask;
            return assetStore.ClearAsync();
        }
        #endregion

        #region Private - Disposed Guard
        /// <summary> 폐기된 provider 로 들어온 요청이면 경고를 남기고 true 를 반환한다. </summary>
        // Dispose 는 cache 의 OnAssetRemoved 구독을 끊는다. 그 뒤로도 조회·해제가 동작하면
        // "캐시에는 등록되는데 로더 핸들은 아무도 회수하지 않는" 상태가 만들어진다.
        // 폐기 이후의 진입은 전부 호출자 측 수명 관리 오류이므로, 조용히 통과시키지 않고
        // 거부 + 경고로 드러낸다 (케이스 리포트 07 NEG-1 / RACE-1 / USR-3).
        private bool _RejectIfDisposed(string apiName) {
            if (!disposed) return false;

            HLogger.Warning(
                $"[AssetProvider] {apiName} called after Dispose. The request is rejected — check the owner's lifetime.");
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

            // await 는 프레임을 넘긴다 — 그 사이 소유자가 파괴되어 Dispose 가 돌았을 수 있다.
            // 진입부 가드만으로는 "요청은 폐기 전, 완료는 폐기 후" 인 in-flight 건을 못 막는다.
            // 이 상태로 _SaveCache 를 하면 구독이 끊긴 캐시에 등록되어 로더 핸들이 영구 잔존한다.
            // (케이스 리포트 07 RACE-1 / RACE-2). 방금 잡은 핸들을 돌려주고 빈손으로 끝낸다.
            if (disposed) {
                HLogger.Warning(
                    $"[AssetProvider] Disposed while loading key '{request.Key}'. Releasing the in-flight loader handle.");
                _ReleaseAssetLoaders(request.Key);
                return default;
            }

            // 점유 등록은 게이트 밖에서 호출자마다 수행한다. 게이트 안(factory)은 최초 호출자
            // 1회만 실행되므로, 안에서 등록하면 dedupe 로 합쳐진 후속 호출자들이 미등록 상태로
            // asset 을 받아 다른 호출자의 Release 한 번에 조기 해제되는 사고가 난다.
            // (익명 경로 포함 — 등록/해제 짝은 호출자 단위로 1:1)
            if (_IsValidAsset(request.Key, asset)) {
                // Save 가 거부되면(같은 키에 다른 asset 이 이미 등록됨) 호출자는 등록되지 않은
                // asset 을 받게 되고, 그 asset 의 로더 핸들은 누구도 해제하지 않는 누수가 된다.
                if (!_SaveCache(request, asset)) {
                    HLogger.Error(
                        $"[AssetProvider] Cache rejected key '{request.Key}'. Releasing the freshly loaded asset to avoid a handle leak.");
                    // 캐시가 소유하지 않으므로 OnAssetRemoved 연쇄가 돌지 않는다 — 로더에 직접 돌려준다.
                    _ReleaseAssetLoaders(request.Key);
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

            await _SaveStoreOrReleaseSourceAsync(request.Key, sourceAsset);
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

            await _SaveStoreOrReleaseSourceAsync(request.Key, sourceAsset);
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
                await _SaveStoreOrReleaseSourceAsync(request.Key, sourceAsset);
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
            // 점유 등록 없이 존재만 확인 — 등록은 _GetAsync 의 게이트 밖 _SaveCache 가 담당한다.
            return assetCache.TryGet(request.Key, out asset);
        }

        private bool _SaveCache(AssetRequest<TKey> request, TAsset asset) {
            if (request.HasOwner) {
                return assetCache.Save(request.Key, asset, request.OwnerId);
            }
            return assetCache.Save(request.Key, asset);
        }

        // 로더가 이미 핸들을 잡은 뒤의 단계다. 여기서 예외가 나가면 _GetAsync 의 _SaveCache 에
        // 도달하지 못해 "캐시 등록은 없는데 로더 핸들만 살아있는" 불일치가 남고, 그 핸들은
        // OnAssetRemoved 연쇄가 돌지 않으므로 아무도 해제하지 않는다.
        // 예외는 삼키지 않고 그대로 올리되, 나가기 전에 방금 잡은 핸들만 되돌려 놓는다.
        private async UniTask _SaveStoreOrReleaseSourceAsync(TKey key, TAsset asset) {
            if (assetStore == null) return;

            try {
                await assetStore.SaveAsync(key, asset);
            }
            catch {
                HLogger.Error(
                    $"[AssetProvider] Store save failed after the source load for key '{key}'. Releasing the loader handle before rethrowing.");
                _ReleaseAssetLoaders(key);
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
 * 2026-08-06 (수정 2) :: 폐기 후 진입 가드 (케이스 리포트 07 NEG-1 / RACE-1 / USR-3)
 * =========================================================
 * 변경 ::
 * 1) 공개 API 9종(GetAsync 2 / TryGet / Release 2 / ReleaseOwner / ReleaseAll / ClearCache /
 *    ClearStoreAsync) 진입부에 _RejectIfDisposed 가드 추가 — 경고 후 무해값 반환.
 * 2) _GetAsync 의 await 재개 직후 disposed 재검사 — in-flight 로 잡은 로더 핸들 회수 후 종료.
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
 * =========================================================
 * 변경 ::
 * 1) Dispose 가 구독 해제만 하던 것을 ReleaseAll → 구독 해제 순서로 확장 + 재진입 가드.
 *    IAssetProvider 가 IDisposable 을 상속하도록 바꿔 인터페이스 타입 필드에서도 호출 가능.
 * 2) _SaveStoreAsync 를 _SaveStoreOrReleaseSourceAsync 로 교체 — 저장 실패 시 로더 핸들
 *    회수 후 예외 재던짐. 호출 3지점(CacheFirst / LocalStoreFirst / SourceFirst) 동시 적용.
 *
 * 이유 ::
 * 1) 호출자 0건의 원인은 "부르는 사람이 없다" 가 아니라 소유자들이 전부 IAssetProvider 타입
 *    필드로 들고 있어 IDisposable 이 보이지 않았던 것이다. 계약에 올려 도달 가능하게 했다.
 *    ReleaseAll 을 먼저 태우지 않으면 구독이 끊긴 뒤 남은 점유의 loader 핸들이 영구 잔존한다.
 * 2) 로더가 핸들을 잡은 직후의 await 에서 예외가 나면 _SaveCache 에 도달하지 못해 캐시 등록
 *    없이 핸들만 남는다. OnAssetRemoved 연쇄 대상이 아니므로 아무도 해제하지 않는다.
 *    예외 자체는 삼키지 않는다 (전역 CLAUDE.md 무음 실패 금지) — 핸들만 되돌리고 재던진다.
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
