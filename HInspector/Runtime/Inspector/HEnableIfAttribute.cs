#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * 특정 조건이 만족될 때만 필드를 편집 가능하게 만드는 Attribute입니다.
 * HReadOnly의 반대 동작입니다.
 *
 * 사용 예 ::
 * [HEnableIf(nameof(isEditable))]
 * public int value;
 *
 * 표현식 사용 ::
 * [HEnableIf("@level >= 10")]
 * public int bonusDamage;
 *
 * 동작 ::
 * 조건이 true이면 편집 가능, false이면 ReadOnly
 * =========================================================
 */
#endif

namespace HInspector {
    [System.AttributeUsage(System.AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class HEnableIfAttribute : HInspectorAttribute {
        public string Condition { get; }
        public string Expression { get; }
        public bool IsExpression => !string.IsNullOrEmpty(Expression);

        public HEnableIfAttribute(string condition, int order = 500)
            : base(order) {
            if (!string.IsNullOrEmpty(condition) && condition[0] == '@') {
                Expression = condition;
                Condition = null;
            }
            else {
                Condition = condition;
                Expression = null;
            }
        }
    }
}
