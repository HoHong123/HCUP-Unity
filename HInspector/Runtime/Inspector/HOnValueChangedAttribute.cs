#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * Inspector 값이 변경될 때 특정 메서드를 호출하는 Attribute입니다.
 * 
 * 사용 예 ::
 * [HOnValueChanged(nameof(UpdatePreview))]
 * public int value;
 * =========================================================
 */

namespace HInspector {
    [System.AttributeUsage(System.AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
    public class HOnValueChangedAttribute : HInspectorAttribute {
        public string MethodName { get; }

        // Odin OnValueChanged.IncludeChildren과의 API 호환용 필드.
        // HInspector 드로어는 BeginChangeCheck가 자식 변경까지 자동 감지하므로 기본 동작이 true와 동치다.
        // 이 필드는 주로 Odin 어댑터(HInspectorToOdinBridge)에서 Odin 속성으로 매핑될 때 전달된다.
        public bool IncludeChildren { get; set; } = true;

        public HOnValueChangedAttribute(string methodName, int order = 1000)
            : base(order) {
            MethodName = methodName;
        }
    }
}
#endif