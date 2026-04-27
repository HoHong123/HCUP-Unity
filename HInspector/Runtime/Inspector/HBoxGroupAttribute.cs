#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * Inspector에서 필드들을 하나의 박스 GUI 안에 시각적으로 묶는 Attribute입니다.
 * 동일한 GroupName을 가진 필드들은 박스 프레임 안에 수직으로 정렬되며,
 * GroupName이 박스 상단에 헤더로 표시됩니다.
 *
 * 사용 예 ::
 * [HBoxGroup("Stats")]
 * public int hp;
 * [HBoxGroup("Stats")]
 * public int atk;
 *
 * 결과 ::
 * ┌─ Stats ───┐
 * │ HP  [______]    │
 * │ Atk [______]    │
 * └───────┘
 *
 * 주의사항 ::
 * 내부 필드들을 수평으로 나열하려면 HHorizontalGroupAttribute를 사용하세요.
 * HBoxGroup과 HHorizontalGroup은 직교하는 독립 개념입니다.
 * =========================================================
 */
#endif
#if UNITY_EDITOR
namespace HInspector {
    [System.AttributeUsage(System.AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
    public class HBoxGroupAttribute : HInspectorAttribute {
        public string GroupName { get; }

        public HBoxGroupAttribute(string groupName, int order = -40)
            : base(order) {
            GroupName = groupName;
        }
    }
}
#endif
