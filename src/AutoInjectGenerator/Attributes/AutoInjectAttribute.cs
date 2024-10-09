using System;
using System.Collections.Generic;
using System.Text;

namespace AutoInjectGenerator;

/// <summary>
/// 自动注入
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = true, Inherited = false)]
public class AutoInjectAttribute : Attribute
{
    /// <summary>
    /// 注册实例生命周期，默认Scoped
    /// </summary>
    public InjectLifeTime LifeTime { get; set; } = InjectLifeTime.Scoped;
    /// <summary>
    /// 注册服务对应服务类型，默认是自身
    /// </summary>
    public Type? ServiceType { get; set; }
    /// <summary>
    /// 配置所属组别, 适用于<see cref="AutoInjectConfiguration.Exclude"/>或者<see cref="AutoInjectConfiguration.Include"/>
    /// </summary>
    public string? Group { get; set; }
    /// <summary>
    /// 键值注册
    /// </summary>
    public string? ServiceKey { get; set; }
    /// <summary>
    /// 是否使用TryAdd
    /// </summary>
    public bool IsTry { get; set; }
}

/// <summary>
/// 注册为HostedService, 未实现
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class AutoInjectHostedAttribute : Attribute
{

}