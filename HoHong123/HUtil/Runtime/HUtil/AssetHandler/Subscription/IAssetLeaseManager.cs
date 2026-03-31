using Cysharp.Threading.Tasks;
using HUtil.AssetHandler.Data;

#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * lease 발급 계약 인터페이스 스크립트입니다.
 *
 * 주의사항 ::
 * 1. owner 객체와 ownerId 직접 전달 경로를 구분해서 사용해야 합니다.
 * 2. lease 계층은 선택 기능이라는 점을 전제로 합니다.
 * =========================================================
 */
#endif

namespace HUtil.AssetHandler.Subscription {
    public interface IAssetLeaseManager<TKey, TAsset> {
        UniTask<IAssetLease<TKey, TAsset>> AcquireAsync(
            IAssetOwner owner,
            TKey key,
            AssetLoadMode loadMode,
            AssetFetchMode fetchMode = AssetFetchMode.CacheFirst);

        UniTask<IAssetLease<TKey, TAsset>> AcquireAsync(
            AssetOwnerId ownerId,
            TKey key,
            AssetLoadMode loadMode,
            AssetFetchMode fetchMode = AssetFetchMode.CacheFirst);
    }
}

#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * 주요 기능 ::
 * 1. IAssetOwner 기준 AcquireAsync를 제공합니다.
 * 2. AssetOwnerId 직접 지정 AcquireAsync를 제공합니다.
 *
 * 사용법 ::
 * 1. 구독형 asset 사용 표현이 필요한 경우에만 참조합니다.
 * 2. provider 직접 사용보다 상위 표현이 필요할 때 사용합니다.
 *
 * 이벤트 ::
 * 1. 직접 이벤트는 없습니다.
 * 2. Acquire 결과는 IAssetLease로 반환됩니다.
 *
 * 기타 ::
 * 1. 일반 provider 경계를 래핑합니다.
 * 2. 수명 관리를 Dispose 패턴으로 드러내기 위한 계약입니다.
 * =========================================================
 */
#endif
