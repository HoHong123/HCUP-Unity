#if UNITY_EDITOR
/* =========================================================
 * @Jason - PKH
 * Inspector에서 Min / Max 슬라이더 UI를 제공하는 Attribute입니다.
 *
 * 적용 타입 ::
 * float, int, Vector2
 *
 * Vector2의 경우 ::
 * x = Min, y = Max
 *
 * 사용 예 ::
 * [HMinMaxSlider(0, 100)]
 * public Vector2 damageRange;
 * =========================================================
 */
#endif

namespace HUtil.Inspector {
    [System.AttributeUsage(System.AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
    public class HMinMaxSliderAttribute : HInspectorAttribute {
        public float Min { get; }
        public float Max { get; }

        public HMinMaxSliderAttribute(float min, float max, int order = 110)
            : base(order) {
            Min = min;
            Max = max;
        }
    }
}
