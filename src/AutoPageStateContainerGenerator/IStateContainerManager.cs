using Microsoft.Extensions.DependencyInjection;
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
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    internal T? InternalGetStateContainer<T>() where T : class;
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
    public static TState? GetStateContainer<TState>(this IStateContainerManager manager, string? name = null)
        where TState : class
    {
        if (name is not null)
            return manager.GetStateContainer(name) as TState;
        else
        {
            return manager.InternalGetStateContainer<TState>();
        }
    }
}


internal sealed class DefaultStateContainer(IServiceProvider services) : IStateContainerManager
{
    private static readonly ConcurrentDictionary<string, Type> types = [];
    private static readonly ConcurrentDictionary<Type, Type> typeMaps = [];
    //private readonly IServiceScopeFactory scopeFactory;

    public static void Add(string name, Type type)
    {
        types.TryAdd(name, type);
    }

    public static void Add(string? name, Type type, Type interfaceType)
    {
        if (name is not null)
            types.TryAdd(name, type);
        if (interfaceType != null && interfaceType.IsAssignableFrom(type))
        {
            typeMaps.TryAdd(interfaceType, type);
        }
    }

    public IGeneratedStateContainer? GetStateContainer(string name)
    {
        if (types.TryGetValue(name, out var type))
        {
            return services.GetService(type) as IGeneratedStateContainer;
        }
        return null;
    }

    public T? InternalGetStateContainer<T>()
        where T : class
    {
        var t = typeof(T);
        if (typeMaps.TryGetValue(t, out var implType))
        {
            return services.GetService(implType) as T;
        }
        foreach (var item in types.Values)
        {
            if (t.IsAssignableFrom(item))
            {
                return services.GetService(item) as T;
            }
        }

        return null;
    }
}