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
