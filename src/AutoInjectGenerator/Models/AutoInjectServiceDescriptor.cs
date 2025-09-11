using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace AutoInjectGenerator.Models;

/// <summary>
/// 包含工厂注册的类型信息
/// </summary>
public class AutoInjectServiceDescriptor : ServiceDescriptor
{
    private readonly Type? factoryReturnType;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="serviceType"></param>
    /// <param name="instance"></param>
    public AutoInjectServiceDescriptor(Type serviceType, object instance) : base(serviceType, instance)
    {
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="serviceType"></param>
    /// <param name="implementationType"></param>
    /// <param name="lifetime"></param>
    public AutoInjectServiceDescriptor(Type serviceType, Type implementationType, ServiceLifetime lifetime) : base(serviceType, implementationType, lifetime)
    {
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="serviceType"></param>
    /// <param name="serviceKey"></param>
    /// <param name="instance"></param>
    public AutoInjectServiceDescriptor(Type serviceType, object? serviceKey, object instance) : base(serviceType, serviceKey, instance)
    {
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="serviceType"></param>
    /// <param name="factory"></param>
    /// <param name="lifetime"></param>
    /// <param name="factoryReturnType"></param>
    public AutoInjectServiceDescriptor(Type serviceType, Func<IServiceProvider, object> factory, ServiceLifetime lifetime, Type factoryReturnType) : base(serviceType, factory, lifetime)
    {
        this.factoryReturnType = factoryReturnType;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="serviceType"></param>
    /// <param name="serviceKey"></param>
    /// <param name="implementationType"></param>
    /// <param name="lifetime"></param>
    public AutoInjectServiceDescriptor(Type serviceType, object? serviceKey, Type implementationType, ServiceLifetime lifetime) : base(serviceType, serviceKey, implementationType, lifetime)
    {
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="serviceType"></param>
    /// <param name="serviceKey"></param>
    /// <param name="factory"></param>
    /// <param name="lifetime"></param>
    /// <param name="factoryReturnType"></param>
    public AutoInjectServiceDescriptor(Type serviceType, object? serviceKey, Func<IServiceProvider, object?, object> factory, ServiceLifetime lifetime, Type factoryReturnType) : base(serviceType, serviceKey, factory, lifetime)
    {
        this.factoryReturnType = factoryReturnType;
    }

    /// <summary>
    /// 
    /// </summary>
    public Type? FactoryReturnType => factoryReturnType;
}
