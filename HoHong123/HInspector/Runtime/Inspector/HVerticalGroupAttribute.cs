#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * Inspector에서 동일 GroupName을 가진 필드들을 수직으로 묶는 Attribute입니다.
 * 시각 장식 없이 순수 레이아웃만 담당합니다.
 *
 * 사용 예 ::
 * [HVerticalGroup("Column1")]
 * public int a;
 * [HVerticalGroup("Column1")]
 * public int b;
 *
 * 결과 ::
 * A [______]
 * B [______]
 *
 * 주의사항 ::
 * 단독 사용 시 시각 효과는 기본 인스펙터와 동일하지만, 그룹 식별자가
 * 유지되어 추후 HBoxGroup 장식이나 중첩 레이아웃의 기반이 됩니다.
 * HHorizontalGroup과 인접 배치하면 열/행이 교차하는 구조를 만들 수 있습니다.
 * =========================================================
 */
#endif

namespace HInspector {
    [System.AttributeUsage(System.AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class HVerticalGroupAttribute : HInspectorAttribute {
        public string GroupName { get; }

        public HVerticalGroupAttribute(string groupName, int order = -40)
            : base(order) {
            GroupName = groupName;
        }
    }
}
