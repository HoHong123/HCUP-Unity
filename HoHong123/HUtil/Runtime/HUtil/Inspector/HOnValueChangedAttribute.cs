namespace HUtil.Inspector {
    [System.AttributeUsage(System.AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
    public class HOnValueChangedAttribute : HInspectorAttribute {
        public string MethodName { get; }

        public HOnValueChangedAttribute(string methodName, int order = 1000)
            : base(order) {
            MethodName = methodName;
        }
    }
}
