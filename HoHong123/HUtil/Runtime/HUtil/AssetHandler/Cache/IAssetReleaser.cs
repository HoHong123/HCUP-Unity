using HUtil.AssetHandler.Subscription;

#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * AssetHandler 캐시 해제 계약 인터페이스 스크립트입니다.
 *
 * 주의사항 ::
 * 1. key 해제와 owner 해제의 의미를 구현체에서 명확히 나눠야 합니다.
 * 2. ReleaseAll과 Clear의 차이를 구현체가 일관되게 유지해야 합니다.
 * =========================================================
 */
#endif

namespace HUtil.AssetHandler.Cache {
    public interface IAssetReleaser<TKey> {
        bool Release(TKey key);
        bool Release(TKey key, AssetOwnerId ownerId);
        int ReleaseOwner(AssetOwnerId ownerId);
        void ReleaseAll();
        void Clear();
    }
}


