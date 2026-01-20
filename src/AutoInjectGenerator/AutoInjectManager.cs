using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace AutoInjectGenerator;

/// <summary>
/// 
/// </summary>
public readonly struct AutoInjectConfiguration(List<string> ExcludeGroups, List<string> IncludeGroups)
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="group"></param>
    /// <returns></returns>
    public readonly bool ShouldInject(string group)
    {
        if (string.IsNullOrWhiteSpace(group))
        {
            return true;
        }
        // 没有配置规则，全部注入
        if (IncludeGroups.Count == 0 && ExcludeGroups.Count == 0)
        {
            return true;
        }
        /*
         * 配置了Group属性
         * 1. 不在excludes中才能注册
         * 2. 存在includes中才能注册
         */
        if (ExcludeGroups.Contains(group) || !IncludeGroups.Contains(group))
        {
            return false;
        }
        return true;
    }
}

/// <summary>
/// 
/// </summary>
[Obsolete]
public class AutoInjectManager
{
    private static readonly ConcurrentDictionary<string, Action<IServiceCollection, AutoInjectConfiguration>> projectServices = [];
    /// <summary>
    /// 
    /// </summary>
    /// <param name="projectName"></param>
    /// <param name="registerAction"></param>
    public static void RegisterProjectServices(string projectName, Action<IServiceCollection, AutoInjectConfiguration> registerAction)
    {
        projectServices.TryAdd(projectName, registerAction);
    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="services"></param>
    /// <param name="config"></param>
    public static void ApplyProjectServices(IServiceCollection services, AutoInjectConfiguration config)
    {
        foreach (var item in projectServices.Values)
        {
            item.Invoke(services, config);
        }
    }
}

