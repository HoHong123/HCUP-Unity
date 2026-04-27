#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * Inspector 필드의 최대값을 제한하는 Attribute입니다.
 *
 * 적용 타입 ::
 * int, float, Vector2
 *
 * 사용 예 ::
 * [HMax(100)]
 * public int hp;
 * =========================================================
 */

namespace HInspector {
    [System.AttributeUsage(System.AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
    public class HMaxAttribute : HInspectorAttribute {
        public float Max { get; }

        public HMaxAttribute(float max, int order = 100)
            : base(order) {
            Max = max;
        }
    }
}
#endif