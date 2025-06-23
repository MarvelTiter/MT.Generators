using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoPageStateContainerGenerator;

/// <summary>
/// 标记需要生成数据的容器的组件
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class StateContainerAttribute : Attribute
{
    /// <summary>
    /// 生命周期
    /// </summary>
    public ServiceLifetime Lifetime { get; set; }
    /// <summary>
    /// 容器名称, 用于<see cref="IStateContainerManager.GetStateContainer(string)"/> 和属性命名
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// 自定义实现类型, 配合<see cref="IStateContainerManager.GetStateContainer(string)"/>使用
    /// </summary>
    public Type? Implements { get; set; }
}