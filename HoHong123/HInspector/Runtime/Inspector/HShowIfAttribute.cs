#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * Inspector에서 특정 조건이 만족될 때만 필드를 표시하는 Attribute입니다.
 *
 * 지원 조건 ::
 * bool
 * 숫자 비교
 * enum 비교
 *
 * 사용 예 ::
 * [HShowIf(nameof(isAdvanced))]
 * public int advancedValue;
 * [HShowIf(nameof(level), 10, HCompareType.GreaterOrEqual)]
 * public int bossDamage;
 *
 * 내부 동작 ::
 * MemberName 값을 reflection으로 조회 후 CompareValue와 비교합니다.
 * =========================================================
 */
#endif

namespace HInspector {
    [System.AttributeUsage(System.AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
    public class HShowIfAttribute : HInspectorAttribute {
        public string MemberName { get; }
        public string Expression { get; }
        public object CompareValue { get; }
        public HCompareType CompareType { get; }
        public bool HasCompareValue { get; }
        public bool IsExpression => !string.IsNullOrEmpty(Expression);

        public HShowIfAttribute(string condition, int order = -100)
            : base(order) {
            if (!string.IsNullOrEmpty(condition) && condition[0] == '@') {
                Expression = condition;
                MemberName = null;
            }
            else {
                MemberName = condition;
                Expression = null;
            }

            CompareType = HCompareType.Equals;
            CompareValue = null;
            HasCompareValue = false;
        }

        public HShowIfAttribute(string memberName, object compareValue, HCompareType compareType = HCompareType.Equals, int order = -100)
            : base(order) {
            MemberName = memberName;
            Expression = null;
            CompareValue = compareValue;
            CompareType = compareType;
            HasCompareValue = true;
        }
    }
}