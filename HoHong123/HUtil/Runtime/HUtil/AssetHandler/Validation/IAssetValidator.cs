#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * key와 asset 유효성 검사 계약 인터페이스 스크립트입니다.
 *
 * 주의사항 ::
 * 1. 입력 검증과 정책 판단을 혼동하지 않도록 역할을 좁게 유지해야 합니다.
 * 2. provider와 loader가 신뢰할 최소 조건만 보장하는 경계입니다.
 * =========================================================
 */
#endif

namespace HUtil.AssetHandler.Validation {
    public interface IAssetValidator<TKey, TAsset> {
        bool CanLoad(TKey key);
        bool IsValid(TKey key, TAsset asset);
    }
}
