#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * Inspector 필드의 최소값을 제한하는 Attribute입니다.
 *
 * 적용 타입 ::
 * int, float, Vector2
 *
 * 사용 예 ::
 * [HMin(0)]
 * public int hp;
 * =========================================================
 */

namespace HUtil.Inspector {
    [System.AttributeUsage(System.AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
    public class HMinAttribute : HInspectorAttribute {
        public float Min { get; }

        public HMinAttribute(float min, int order = 100)
            : base(order) {
            Min = min;
        }
    }
}       
#endif