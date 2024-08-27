using System;

namespace AutoAopProxyGenerator;

/// <summary>
/// 标记需要生成代理类的类型
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class GenAspectProxyAttribute : Attribute
{

}
