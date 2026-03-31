#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * AssetProvider의 조회 우선순위를 정의하는 열거형 스크립트입니다.
 *
 * 주의사항 ::
 * 1. load mode와 다른 개념이므로 혼동하지 않아야 합니다.
 * 2. provider는 이 값에 따라 cache, store, source 순서를 바꿉니다.
 * =========================================================
 */
#endif

namespace HUtil.AssetHandler.Data {
    public enum AssetFetchMode : byte {
        CacheFirst = 0,
        LocalStoreFirst = 1,
        LocalStoreOnly = 2,
        SourceFirst = 3,
        SourceOnly = 4,
    }
}
