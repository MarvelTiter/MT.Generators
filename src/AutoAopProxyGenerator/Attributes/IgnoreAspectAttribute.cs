using System;

namespace AutoAopProxyGenerator;

/// <summary>
/// 配置不需要切面的方法
/// <para>
/// 构造函数可选指定忽略的<see cref="IAspectHandler"/>，默认忽略全部
/// </para>
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class IgnoreAspectAttribute : Attribute
{
    private readonly Type[] ignoreTypes;

    /// <summary>
    /// 指定忽略的<see cref="IAspectHandler"/>
    /// </summary>
    /// <param name="ignoreTypes"></param>
    public IgnoreAspectAttribute(params Type[] ignoreTypes)
    {
        this.ignoreTypes = ignoreTypes;
    }
}
