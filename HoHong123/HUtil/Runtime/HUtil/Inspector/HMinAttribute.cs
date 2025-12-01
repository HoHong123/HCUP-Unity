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