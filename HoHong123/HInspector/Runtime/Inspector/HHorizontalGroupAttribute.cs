#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * Inspector에서 동일 GroupName을 가진 필드들을 수평으로 나열하는 Attribute입니다.
 * 시각 장식 없이 순수 레이아웃만 담당합니다.
 *
 * 사용 예 ::
 * [HHorizontalGroup("Row1")]
 * public int a;
 * [HHorizontalGroup("Row1")]
 * public int b;
 *
 * 결과 ::
 * A [______]  B [______]
 *
 * 주의사항 ::
 * 연속 배치된 같은 GroupName만 하나의 행으로 묶입니다.
 * 시각적 박스 프레임이 필요하면 HBoxGroup(추후)을, 수직 묶음이 필요하면
 * HVerticalGroup을 사용하세요.
 * =========================================================
 */
#endif

namespace HInspector {
    [System.AttributeUsage(System.AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class HHorizontalGroupAttribute : HInspectorAttribute {
        public string GroupName { get; }

        public HHorizontalGroupAttribute(string groupName, int order = -40)
            : base(order) {
            GroupName = groupName;
        }
    }
}
