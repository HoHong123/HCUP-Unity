#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * Inspector 필드를 읽기 전용(ReadOnly)으로 만드는 Attribute입니다.
 * Inspector에서 값을 표시하지만 수정은 불가능합니다.
 *
 * 사용 예 ::
 * [HReadOnly]
 * public int currentLevel;
 *
 * 조건 기반 ReadOnly ::
 * [HReadOnly(nameof(isLocked))]
 * public int value;
 *
 * Inverse 사용 ::
 * [HReadOnly(nameof(isEditable), true)]
 *
 * 동작 ::
 * 조건이 true이면 ReadOnly 적용
 * inverse=true일 경우 반대로 적용
 * =========================================================
 */
#endif

namespace HUtil.Inspector {
    public class HReadOnlyAttribute : HInspectorAttribute {
        public string ConditionMemberName { get; }
        public bool Inverse { get; }

        public HReadOnlyAttribute(int order = 500) : base(order) { }
        public HReadOnlyAttribute(string conditionMemberName, bool inverse = false, int order = 500)
            : base(order) {
            ConditionMemberName = conditionMemberName;
            Inverse = inverse;
        }
    }
}
