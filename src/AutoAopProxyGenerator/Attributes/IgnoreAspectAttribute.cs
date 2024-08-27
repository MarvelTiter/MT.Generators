using System;

namespace AutoAopProxyGenerator;

/// <summary>
/// 配置不需要切面的方法
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class IgnoreAspectAttribute : Attribute
{

}
