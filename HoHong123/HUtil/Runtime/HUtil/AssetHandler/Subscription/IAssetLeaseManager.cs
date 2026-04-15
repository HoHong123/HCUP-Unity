using Cysharp.Threading.Tasks;
using HUtil.AssetHandler.Data;

#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * lease 발급 계약 인터페이스 스크립트입니다.
 * IAssetProvider의 Owner 기능 구독을 강제하여 메모리 리크를 방지하도록 만드는 선택 계층입니다.
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
 * 3. 역할 제한: Acquire/Dispose 짝맞춤(단일 key 단위)만 제공합니다.
 *    오너 단위 일괄 해제(ReleaseOwner)나 전역 해제(ReleaseAll)는 제공하지 않으며,
 *    해당 경로가 필요하면 IAssetProvider를 직접 호출해야 합니다.
 * 4. provider를 감추는 파사드가 아니라 Dispose 편의 표현이므로,
 *    두 경계를 함께 참조하는 사용처(예: 오너가 OnDestroy에서 lease.Dispose + provider.ReleaseOwner)가 자연스럽습니다.
 * =========================================================
 */
#endif
