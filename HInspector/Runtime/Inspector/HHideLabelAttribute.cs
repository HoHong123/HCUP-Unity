#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * Inspector 필드의 라벨을 숨기는 Attribute입니다.
 * 필드 값만 표시되고 라벨 영역이 제거됩니다.
 *
 * 사용 예 ::
 * [HHideLabel]
 * public string description;
 *
 * 주의사항 ::
 * 로직이 없는 순수 UI 장식용 Attribute이기에 Conditional이 붙습니다.
 * =========================================================
 */
#endif

namespace HInspector {
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    [System.AttributeUsage(System.AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class HHideLabelAttribute : HInspectorAttribute {
        public HHideLabelAttribute(int order = -30)
            : base(order) { }
    }
}
