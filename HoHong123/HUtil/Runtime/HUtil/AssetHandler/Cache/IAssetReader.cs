using HUtil.AssetHandler.Subscription;

#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * AssetHandler 캐시 조회 계약 인터페이스 스크립트입니다.
 *
 * 주의사항 ::
 * 1. TryLoad와 TryGet의 의미를 구현체에서 일관되게 유지해야 합니다.
 * 2. owner가 있는 조회는 점유 연결까지 동반할 수 있습니다.
 * =========================================================
 */
#endif

namespace HUtil.AssetHandler.Cache {
    public interface IAssetReader<TKey, TAsset> {
        bool TryLoad(TKey key, out TAsset asset);
        bool TryLoad(TKey key, AssetOwnerId ownerId, out TAsset asset);
        bool TryGet(TKey key, out TAsset asset);
    }
}


