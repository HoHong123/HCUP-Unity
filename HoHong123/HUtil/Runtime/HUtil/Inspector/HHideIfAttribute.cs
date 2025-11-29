namespace HUtil.Inspector {
    [System.AttributeUsage(System.AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
    public class HHideIfAttribute : HShowIfAttribute {
        public HHideIfAttribute(string memberName, int order = -100) : base(memberName, order) {}
        public HHideIfAttribute(string memberName, object compareValue, HCompareType compareType = HCompareType.Equals, int order = -100)
            : base(memberName, compareValue, compareType, order) {}
    }
}