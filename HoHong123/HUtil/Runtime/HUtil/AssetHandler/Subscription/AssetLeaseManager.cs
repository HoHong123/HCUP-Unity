using Cysharp.Threading.Tasks;
using UnityEngine.Assertions;
using HUtil.AssetHandler.Data;
using HUtil.AssetHandler.Provider;

#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * 선택적 lease 계층을 제공하는 매니저. owner-aware provider 위에 얹는 IDisposable 표현 래퍼.
 *
 * 주요 기능 ::
 * AcquireAsync(IAssetOwner / AssetOwnerId, key, loadMode, fetchMode) — lease 발급.
 * lease.Dispose () 시 provider.Release(key, ownerId) 자동 위임 (nested AssetLease 클래스).
 *
 * 사용법 ::
 * 구독형 acquire/release 표현이 필요한 경우에만 사용. 기본 구조만 필요하면 provider 와
 * ownerId 직접 사용. lease 구현은 nested 클래스로 감춰져 외부 노출 0.
 *
 * 주의 ::
 * 현재 구조의 필수 계층 아님 — 선택 기능. Dispose 미호출 시 owner release 의도와 어긋남
 * (using 블록 또는 명시 Dispose 권장).
 * =========================================================
 */
#endif

namespace HUtil.AssetHandler.Subscription {
    public sealed class AssetLeaseManager<TKey, TAsset> : IAssetLeaseManager<TKey, TAsset> {
        #region Nested Types
        sealed class AssetLease : IAssetLease<TKey, TAsset> {
            readonly IAssetProvider<TKey, TAsset> assetProvider;
            bool isDisposed;

            public TKey Key { get; }
            public TAsset Asset { get; }
            public AssetOwnerId OwnerId { get; }
            public bool IsValid => !isDisposed && OwnerId.IsValid;

            public AssetLease(
                IAssetProvider<TKey, TAsset> assetProvider,
                TKey key,
                TAsset asset,
                AssetOwnerId ownerId) {

                this.assetProvider = assetProvider;
                Key = key;
                Asset = asset;
                OwnerId = ownerId;
            }

            public void Dispose() {
                if (isDisposed)
                    return;

                isDisposed = true;
                assetProvider.Release(Key, OwnerId);
            }
        }
        #endregion

        #region Fields
        readonly IAssetProvider<TKey, TAsset> assetProvider;
        #endregion

        #region Public - Constructors
        public AssetLeaseManager(IAssetProvider<TKey, TAsset> assetProvider) {
            Assert.IsNotNull(assetProvider, "[AssetLeaseManager] assetProvider is null.");
            this.assetProvider = assetProvider;
        }
        #endregion

        #region Public - Acquire
        public UniTask<IAssetLease<TKey, TAsset>> AcquireAsync(
            IAssetOwner owner,
            TKey key,
            AssetLoadMode loadMode,
            AssetFetchMode fetchMode = AssetFetchMode.CacheFirst) {

            Assert.IsNotNull(owner, "[AssetLeaseManager] owner is null.");
            return AcquireAsync(owner.OwnerId, key, loadMode, fetchMode);
        }

        public async UniTask<IAssetLease<TKey, TAsset>> AcquireAsync(
            AssetOwnerId ownerId,
            TKey key,
            AssetLoadMode loadMode,
            AssetFetchMode fetchMode = AssetFetchMode.CacheFirst) {

            Assert.IsTrue(ownerId.IsValid, "[AssetLeaseManager] ownerId is invalid.");

            var asset = await assetProvider.GetAsync(
                key: key,
                loadMode: loadMode,
                fetchMode: fetchMode,
                ownerId: ownerId);

            if (Equals(asset, default(TAsset)))
                return null;

            return new AssetLease(assetProvider, key, asset, ownerId);
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
 * 기존 헤더 (상단 도입+주의사항 + 하단 주요기능/사용법/이벤트/기타) 를 한 곳에 통합하여
 * §11 형틀 통일. 하단 Dev Log 영역 추가. 헤더와 Dev Log 모두 #if UNITY_EDITOR 가드.
 *
 * 이유 ::
 * 글로벌 CLAUDE.md §11 룰 일괄 적용.
 *
 * =========================================================
 * 2026-04-25 (최초 설계) :: AssetLeaseManager 초기 구현
 * =========================================================
 * IDisposable 패턴으로 acquire/release 짝맞춤을 컴파일러가 강제하게 만드는 표현 계층.
 * nested AssetLease 클래스가 isDisposed 가드 + provider.Release(Key, OwnerId) 위임으로
 * 단일 Dispose 책임만 수행. lease 자체는 자산을 복제 소유하지 않음 — 실제 reference
 * counting 은 provider(cache) 한 곳. 두 AcquireAsync 오버로드 (IAssetOwner / ownerId 직접) 로
 * 호출자 의도 표현 자유도 확보. asset 가 default(TAsset) 면 lease 발급 없이 null 반환 —
 * acquire 실패 케이스를 명시적으로 표현.
 * =========================================================
 */
#endif
