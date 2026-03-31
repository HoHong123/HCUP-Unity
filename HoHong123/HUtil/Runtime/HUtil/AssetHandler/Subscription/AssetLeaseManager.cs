using Cysharp.Threading.Tasks;
using UnityEngine.Assertions;
using HUtil.AssetHandler.Data;
using HUtil.AssetHandler.Provider;

#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * 선택적 lease 계층을 제공하는 매니저 스크립트입니다.
 *
 * 주의사항 ::
 * 1. 현재 구조의 필수 계층은 아니며 선택 기능입니다.
 * 2. Dispose를 호출하지 않으면 owner release 의도와 어긋날 수 있습니다.
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
 * @Jason - PKH
 * 주요 기능 ::
 * 1. AcquireAsync로 lease를 발급합니다.
 * 2. lease dispose 시 provider release를 연결합니다.
 *
 * 사용법 ::
 * 1. 구독형 acquire/release 표현이 필요한 경우에만 사용합니다.
 * 2. 기본 구조만 필요하면 provider와 ownerId만 사용해도 됩니다.
 *
 * 이벤트 ::
 * 1. 별도의 공개 이벤트는 없습니다.
 * 2. lease dispose 시 내부적으로 release 호출이 이어집니다.
 *
 * 기타 ::
 * 1. lease 구현은 nested 클래스로 감춥니다.
 * 2. owner-aware provider 위에 얹는 보조 계층입니다.
 * =========================================================
 */
#endif
