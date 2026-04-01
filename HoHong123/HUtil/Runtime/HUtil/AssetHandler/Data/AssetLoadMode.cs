#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * 이 스크립트는 Asset source 종류를 구분하는 열거형 스크립트입니다.
 *
 * 주의사항 ::
 * 1. fetch mode와 다른 개념입니다.
 * 2. loader 선택 기준으로 사용되므로 확장 시 provider 매핑도 함께 봐야 합니다.
 * =========================================================
 */
#endif

namespace HUtil.AssetHandler.Data {
    public enum AssetLoadMode : byte {
        Resources = 0,
        Addressable = 1,
    }
}
