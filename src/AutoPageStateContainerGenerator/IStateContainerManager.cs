using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoPageStateContainerGenerator;

/// <summary>
/// 数据容器管理
/// </summary>
public interface IStateContainerManager
{
    /// <summary>
    /// 当前作用域下获取数据容器
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    IGeneratedStateContainer? GetStateContainer(string name);
}

/// <summary>
/// 
/// </summary>
public static class IStateContainerManagerEx
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TState"></typeparam>
    /// <param name="manager"></param>
    /// <param name="name"></param>
    /// <returns></returns>
    public static TState? GetStateContainer<TState>(this IStateContainerManager manager, string name)
        where TState : class
    {
        return manager.GetStateContainer(name) as TState;
    }
}


internal class DefaultStateContainer(IServiceProvider services) : IStateContainerManager
{
    private static readonly ConcurrentDictionary<string, Type> types = [];
    public static void Add(string name, Type type)
    {
        types.TryAdd(name, type);
    }
    public IGeneratedStateContainer? GetStateContainer(string name)
    {
        if (!types.TryGetValue(name, out var type))
        {
            return null;
        }
        return services.GetService(type) as IGeneratedStateContainer;
    }
}