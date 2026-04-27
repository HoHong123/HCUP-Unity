#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * 메서드를 Inspector 하단의 버튼으로 노출시키는 Attribute입니다.
 * 선언 순서와 무관하게 모든 필드 렌더링이 끝난 뒤 하단에 누적 배치됩니다.
 *
 * 사용 예 ::
 * [HButton]
 * private void ResetStats() { ... }
 *
 * [HButton("체력 회복")]
 * private void _Heal() { ... }
 *
 * 결과 ::
 * (필드 영역)
 * ─────────────────
 * [ ResetStats ]
 * [ 체력 회복 ]
 *
 * 특징 ::
 * Label 파라미터가 null/빈 문자열이면 메서드명을 그대로 라벨로 사용합니다.
 * 파라미터가 없는 메서드만 지원합니다. (매개변수 UI는 후속 단계에서 추가 예정)
 * 다중 선택 편집(Multi-Object Editing) 상황에서는 targets 배열을 순회해
 * 모든 대상에서 메서드를 호출합니다.
 *
 * 주의사항 ::
 * HInspectorEditor(CustomEditor)가 처리하므로 HInspectorBehaviour 또는
 * HInspectorScriptableObject를 상속받은 타겟에서만 시각적으로 그려집니다.
 * 일반 MonoBehaviour / ScriptableObject에서는 어트리뷰트가 무시됩니다.
 * Field 전용인 HInspectorAttribute 계열이 아닌, Method 전용 Attribute이기에
 * PropertyDrawer 파이프라인을 우회하고 CustomEditor에서만 수집/렌더됩니다.
 * =========================================================
 */
#endif

namespace HInspector {
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    [System.AttributeUsage(System.AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class HButtonAttribute : System.Attribute {
        public string Label { get; }
        public int Order { get; }
        public HButtonAttribute(string label = null, int order = 0) {
            Label = label;
            Order = order;
        }
    }
}
