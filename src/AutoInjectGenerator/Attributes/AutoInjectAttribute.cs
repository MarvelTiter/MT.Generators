using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace AutoInjectGenerator;

/// <summary>
/// 自动注入
/// <para>当直接实现的接口只有一个时，<see cref="ServiceType"/>就是该接口</para>
/// <para>否则不指定<see cref="ServiceType"/>的话，就是注入自身</para>
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
    /// 配置所属组别, 适用于<see cref="AutoInjectConfigurationAttribute.Exclude"/>或者<see cref="AutoInjectConfigurationAttribute.Include"/>
    /// </summary>
    public string? Group { get; set; }
    /// <summary>
    /// 键值注册，只支持使用string类型，其他类型不做判断
    /// </summary>
    public string? ServiceKey { get; set; }
    /// <summary>
    /// 是否使用TryAdd
    /// </summary>
    [Obsolete]
    public bool IsTry { get; set; }

    /// <summary>
    /// <strong>静态</strong>自定义工厂方法，方法签名为Func&lt;IServiceProvider, object&gt;，返回值为要注册的服务实例
    /// </summary>
    public string? Factory { get; set; }

    /// <summary>
    /// <strong>静态</strong>自定义实例
    /// </summary>
    public string? Instance { get; set; }

    /// <summary>
    /// <see cref="Factory"/> 或者<see cref="Instance"/>的声明所在类型，默认为当前类型
    /// </summary>
    public Type? DeclaredType { get; set; }
}

/// <summary>
/// 注册为HostedService, 未实现
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class AutoInjectHostedAttribute : Attribute
{

}
