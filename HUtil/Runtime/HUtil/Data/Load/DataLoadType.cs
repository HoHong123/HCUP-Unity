#if UNITY_EDITOR
/* =========================================================
 * 데이터를 로드하는 소스 유형을 정의하는 열거형입니다.
 *
 * 주의사항 ::
 * 각 Loader 시스템은 해당 Type에 맞는 IDataLoad 구현체를 사용합니다.
 * =========================================================
 */
#endif

namespace HUtil.Data.Load {
    public enum DataLoadType : byte {
        Resources,
        Addressable,
        Local,
        Server,
        PlayerPrefs,
    }
}

#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * 기타 ::
 * 1. DataLoader에서 Loader 선택 기준으로 사용됩니다.
 * =========================================================
 */
#endif