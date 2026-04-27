#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * 직렬화되지 않는 멤버(Property, 비직렬화 Field, Method 리턴값)를 Inspector에
 * 읽기 전용으로 노출시키는 Attribute입니다.
 *
 * 사용 예 ::
 * [HShowInInspector]
 * public float CurrentSpeed => rigidbody.velocity.magnitude;
 *
 * [HShowInInspector("체력 %")]
 * private int _hpPercent => (int)(hp / maxHp * 100);
 *
 * 결과 ::
 * 일반 필드 영역 아래, HButton 영역 다음에 "Current Speed : 2.34" 같은
 * 읽기 전용 값이 Inspector 하단에 표시됩니다.
 *
 * 특징 ::
 * Label이 null/빈 문자열이면 멤버명을 Nicify하여 라벨로 사용합니다.
 * 반영 주기는 매 OnInspectorGUI 호출마다 (Layout/Repaint 등 프레임 여러 번).
 * 따라서 가벼운 getter만 사용할 것을 권장. 무거운 계산은 HButton으로 수동 갱신.
 *
 * 지원 타입 (MVP) ::
 * int, float, bool, string, Vector2/3/4, Color, UnityEngine.Object, Enum
 * 복합 객체/컬렉션/Dictionary는 ToString() 폴백으로 표시됩니다.
 *
 * 주의사항 ::
 * HInspectorEditor(CustomEditor)가 처리하므로 HInspectorBehaviour 또는
 * HInspectorScriptableObject를 상속받은 타겟에서만 시각적으로 그려집니다.
 * Field 전용인 HInspectorAttribute 계열이 아닌 독립 Method/Property/Field
 * 전용 Attribute이기에 System.Attribute를 직접 상속합니다.
 * MVP 스코프: Field + Property만 지원. Method 리턴값 노출은 후속 확장 예정.
 * =========================================================
 */
#endif

namespace HInspector {
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    [System.AttributeUsage(
        System.AttributeTargets.Field | System.AttributeTargets.Property | System.AttributeTargets.Method,
        AllowMultiple = false,
        Inherited = true)]
    public class HShowInInspectorAttribute : System.Attribute {
        public string Label { get; }
        public int Order { get; }
        public HShowInInspectorAttribute(string label = null, int order = 0) {
            Label = label;
            Order = order;
        }
    }
}
