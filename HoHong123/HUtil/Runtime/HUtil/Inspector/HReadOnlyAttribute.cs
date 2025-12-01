namespace HUtil.Inspector {
    public class HReadOnlyAttribute : HInspectorAttribute {
        public string ConditionMemberName { get; }
        public bool Inverse { get; }

        public HReadOnlyAttribute(int order = 500) : base(order) { }
        public HReadOnlyAttribute(string conditionMemberName, bool inverse = false, int order = 500)
            : base(order) {
            ConditionMemberName = conditionMemberName;
            Inverse = inverse;
        }
    }
}
