using System;
using System.Text;

namespace MT.Generators.Abstraction
{
    [AttributeUsage(AttributeTargets.All, AllowMultiple = true, Inherited = false)]
    public class AttachAttributeArgumentAttribute : Attribute
    {
        public string Name { get; set; }
        public object Value { get; set; }
        public string TargetAttribute { get; set; }
        public string TargetGenerator { get; set; }
        public AttachAttributeArgumentAttribute(Type targetGen, Type targetAttr, string name, object value)
        {
            TargetGenerator = targetGen.FullName;
            TargetAttribute = targetAttr.FullName;
            Name = name;
            Value = value;
        }
    }
#if NET7_0_OR_GREATER

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
    public class AttachAttributeArgumentAttribute<T> : Attribute
    {
        public string Name { get; set; }
        public T Value { get; set; }
        public Type TargetAttribute { get; set; }
        public AttachAttributeArgumentAttribute(Type target, string name, T value)
        {
            TargetAttribute = target;
            Name = name;
            Value = value;
        }
    }
#endif

}
