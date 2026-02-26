using System;

namespace AutoGenMapperGenerator;

/// <summary>
/// 忽略映射
/// </summary>
/// <remarks>
/// 
/// </remarks>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Class | AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public class MapIgnoreAttribute(params string[] ignores) : Attribute
{
    /// <summary>
    /// 忽略的列表
    /// </summary>
    public string[] Ignores { get; } = ignores;
}
