#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * HInspector 조건 비교에 사용되는 비교 타입 열거형입니다.
 *
 * 사용 예 ::
 * [HShowIf(nameof(level), 10, HCompareType.GreaterOrEqual)]
 * =========================================================
 */
#endif

namespace HUtil.Inspector {
    public enum HCompareType : byte {
        Equals = 0,
        NotEquals = 1,
        Greater = 2,
        Less = 3,
        GreaterOrEqual = 4,
        LessOrEqual = 5
    }
}
