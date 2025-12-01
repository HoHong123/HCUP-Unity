namespace HUtil.Inspector {
    [System.AttributeUsage(System.AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
    public class HShowIfAttribute : HInspectorAttribute {
        public string MemberName { get; }
        public object CompareValue { get; }
        public HCompareType CompareType { get; }
        public bool HasCompareValue { get; }

        public HShowIfAttribute(string memberName, int order = -100)
            : base(order) {
            MemberName = memberName;
            CompareType = HCompareType.Equals;
            HasCompareValue = false;
        }

        public HShowIfAttribute(string memberName, object compareValue, HCompareType compareType = HCompareType.Equals, int order = -100)
            : base(order) {
            MemberName = memberName;
            CompareType = compareType;
            CompareValue = compareValue;
            HasCompareValue = true;
        }
    }
}