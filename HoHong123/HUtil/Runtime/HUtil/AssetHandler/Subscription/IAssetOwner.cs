#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * OwnerId를 노출하는 최소 owner 계약 인터페이스 스크립트입니다.
 *
 * 주의사항 ::
 * 1. owner 객체의 실제 동작보다 식별자 제공에만 집중해야 합니다.
 * 2. OwnerId 유효성은 구현체 수명과 맞아야 합니다.
 * =========================================================
 */
#endif

namespace HUtil.AssetHandler.Subscription {
    public interface IAssetOwner {
        AssetOwnerId OwnerId { get; }
    }
}
