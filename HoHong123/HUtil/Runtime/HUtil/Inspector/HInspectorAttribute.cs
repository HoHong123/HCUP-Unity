namespace HUtil.Inspector {
    [System.AttributeUsage(System.AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
    public abstract class HInspectorAttribute : UnityEngine.PropertyAttribute {
        public int Order { get; }
        protected HInspectorAttribute(int order = 0) => Order = order;
    }
}
