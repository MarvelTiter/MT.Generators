using Microsoft.Extensions.DependencyInjection;
using System;

namespace AutoPageStateContainerGenerator;

/// <summary>
/// 用于标识生成器生成的数据容器
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class GeneratedStateContainerAttribute : Attribute
{
    /// <summary>
    /// 生命周期
    /// </summary>
    public int Lifetime { get; set; }
    /// <summary>
    /// <see cref="StateContainerAttribute.Name"/>
    /// </summary>
    public string? Name { get; set; }
}