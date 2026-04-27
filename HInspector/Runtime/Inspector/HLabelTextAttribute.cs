#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * Inspector 필드의 라벨 텍스트를 변경하는 Attribute입니다.
 *
 * 사용 예 ::
 * [HLabelText("체력")]
 * public int hp;
 *
 * 결과 ::
 * 기본 "Hp" 대신 "체력"으로 표시
 *
 * 주의사항 ::
 * 로직이 없는 순수 UI 장식용 Attribute이기에 Conditional이 붙습니다.
 * =========================================================
 */
#endif

namespace HInspector {
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    [System.AttributeUsage(System.AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class HLabelTextAttribute : HInspectorAttribute {
        public string Text { get; }

        public HLabelTextAttribute(string text, int order = -30)
            : base(order) {
            Text = text;
        }
    }
}
