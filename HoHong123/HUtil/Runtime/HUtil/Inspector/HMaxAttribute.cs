namespace HUtil.Inspector {
    [System.AttributeUsage(System.AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
    public class HMaxAttribute : HInspectorAttribute {
        public float Max { get; }

        public HMaxAttribute(float max, int order = 100)
            : base(order) {
            Max = max;
        }
    }
}
