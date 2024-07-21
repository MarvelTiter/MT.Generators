using System;
using System.Collections.Generic;
using System.Text;

namespace AutoInjectGenerator;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = true, Inherited = false)]
public class AutoInjectAttribute : Attribute
{
    public InjectLifeTime LifeTime { get; set; } = InjectLifeTime.Scoped;
    public Type? ServiceType { get; set; }
    public string? Group { get; set; }
}

#if NET7_0_OR_GREATER
public class AutoInjectAttribute<T> : AutoInjectAttribute
{
   
}
#endif