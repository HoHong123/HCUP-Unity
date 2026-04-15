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

        public HOnValueChangedAttribute(string methodName, int order = 1000)
            : base(order) {
            MethodName = methodName;
        }
    }
}
#endif