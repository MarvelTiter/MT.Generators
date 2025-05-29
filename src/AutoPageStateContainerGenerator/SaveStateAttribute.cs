using System;

namespace AutoPageStateContainerGenerator;

/// <summary>
/// <para>标记需要保存的字段/属性</para>
/// <para>如果需要设置初始值，需要定义字段，在字段上初始化或者使用<see cref="Init"/></para>
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class SaveStateAttribute : Attribute
{
    /// <summary>
    /// 初始化值
    /// </summary>
    public string? Init { get; set; }
}