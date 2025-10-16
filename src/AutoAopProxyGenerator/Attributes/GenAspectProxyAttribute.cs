using System;

namespace AutoAopProxyGenerator;

/// <summary>
/// 标记需要生成代理类的类型
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class GenAspectProxyAttribute : Attribute
{
    ///// <summary>
    ///// 代理类是否需要继承基类
    ///// </summary>
    //public bool InheritBaseType { get; set; }
}
