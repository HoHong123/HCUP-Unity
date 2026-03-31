using HUtil.AssetHandler.Subscription;

#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * AssetHandler 캐시 저장 계약 인터페이스 스크립트입니다.
 *
 * 주의사항 ::
 * 1. owner가 있는 저장은 점유 등록 정책을 같이 가져가야 합니다.
 * 2. null 또는 invalid asset 처리 기준은 구현체 정책을 따릅니다.
 * =========================================================
 */
#endif

namespace HUtil.AssetHandler.Cache {
    public interface IAssetWriter<TKey, TAsset> {
        bool Save(TKey key, TAsset asset);
        bool Save(TKey key, TAsset asset, AssetOwnerId ownerId);
    }
}


