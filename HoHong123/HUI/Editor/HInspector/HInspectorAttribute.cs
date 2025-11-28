using System;

namespace HUI.HEditor {
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
    public abstract class HInspectorAttribute : Attribute {
        public int Order { get; }
        protected HInspectorAttribute(int order = 0) => Order = order;
    }
}
