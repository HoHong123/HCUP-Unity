namespace HUtil.Inspector {
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    [System.AttributeUsage(System.AttributeTargets.All, AllowMultiple = true, Inherited = true)]
    public class HTitleAttribute : HInspectorAttribute {
        public string Title { get; }
        public HTitleAttribute(string title, int order = -50)
            : base(order) {
            Title = title;
        }
    }
}
