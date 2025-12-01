namespace HUtil.Inspector {
    [System.AttributeUsage(System.AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
    public class HBoxGroupAttribute : HInspectorAttribute {
        public string GroupName { get; }

        public HBoxGroupAttribute(string groupName, int order = -40)
            : base(order) {
            GroupName = groupName;
        }
    }
}