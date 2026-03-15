#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * 특정 조건이 만족될 경우 Inspector에서 필드를 숨깁니다.
 * HShowIf의 반대 동작입니다.
 *
 * 조건 ::
 * bool || 숫자 || enum 비교
 *
 * 사용 예 ::
 * [HHideIf(nameof(isDebugMode))]
 * public int value1;
 * [HHideIf(nameof(level), 10, HCompareType.Greater)]
 * public int value2;
 * =========================================================
 */
#endif

namespace HUtil.Inspector {
    [System.AttributeUsage(System.AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
    public class HHideIfAttribute : HShowIfAttribute {
        public HHideIfAttribute(string condition, int order = -100)
            : base(condition, order) { }
        public HHideIfAttribute(string memberName, object compareValue, HCompareType compareType = HCompareType.Equals, int order = -100)
            : base(memberName, compareValue, compareType, order) { }
    }
}