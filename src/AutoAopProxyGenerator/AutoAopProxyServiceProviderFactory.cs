using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace AutoAopProxyGenerator;

/// <summary></summary>

public class AutoAopProxyServiceProviderFactory : IServiceProviderFactory<IServiceCollection>
{
    private enum ImplType
    {
        None,
        Default,
        Instance,
        Factory
    }

    private readonly struct SDInfo(ServiceDescriptor serviceDescriptor
        , ImplType implType)
    {
        public ServiceDescriptor ServiceDescriptor { get; } = serviceDescriptor;
        public ImplType ImplType { get; } = implType;
        public Type? ImplementationType => ServiceDescriptor.ImplementationType;
        public Type? ServiceType => ServiceDescriptor.ServiceType;
        public ServiceLifetime Lifetime => ServiceDescriptor.Lifetime;
    }

    private readonly record struct SDInfoKey
    {
        public SDInfoKey(
#if NET8_0_OR_GREATER
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
#endif
        Type originType,
#if NET8_0_OR_GREATER
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
#endif
    Type proxyType)
        {
            OriginType = originType;
            ProxyType = proxyType;
        }
#if NET8_0_OR_GREATER
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
#endif
        public Type OriginType { get; }
#if NET8_0_OR_GREATER
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
#endif
        public Type ProxyType { get; }
    }

    /// <summary></summary>
    public IServiceCollection CreateBuilder(IServiceCollection services)
    {
        return services;
    }
    /// <summary></summary>

    public IServiceProvider CreateServiceProvider(IServiceCollection containerBuilder)
    {
        Dictionary<SDInfoKey, List<SDInfo>> needToHandle = [];
        IServiceCollection serviceCollection = new ServiceCollection();
        foreach (var sd in containerBuilder)
        {
            //service.
            var implType = GetImplType(sd, out var it);
            if (implType?.GetCustomAttribute<GenAspectProxyAttribute>() is not null
               && implType?.Assembly.GetType($"{implType.FullName}GeneratedProxy") is { } proxyType)
            {
                //AddServiceDescriptors(serviceCollection, sd, implType, proxyType);
                var key = new SDInfoKey(implType, proxyType);
                if (!needToHandle.TryGetValue(key, out var list))
                {
                    list = [];
                    needToHandle[key] = list;
                }
                list.Add(new(sd, it));
                continue;
            }
            serviceCollection.Add(sd);
        }

        foreach (var kv in needToHandle)
        {
            AddServiceDescriptors(serviceCollection, kv.Key.OriginType, kv.Key.ProxyType, kv.Value);
        }

        return serviceCollection.BuildServiceProvider();
#if NET8_0_OR_GREATER
        [RequiresUnreferencedCode("Uses reflection to load types and attributes.")]
        [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
#endif

        Type? GetImplType(ServiceDescriptor sd, out ImplType implType)
        {
#if NET8_0_OR_GREATER
            if (sd.IsKeyedService)
            {
                if (sd.KeyedImplementationType != null)
                {
                    implType = ImplType.Default;
                    return sd.KeyedImplementationType;
                }
                else if (sd.KeyedImplementationInstance != null)
                {
                    implType = ImplType.Instance;
                    return sd.KeyedImplementationInstance.GetType();
                }
                else if (sd.KeyedImplementationFactory != null)
                {
                    //var typeArguments = sd.KeyedImplementationFactory.GetType().GenericTypeArguments;
                    implType = ImplType.Factory;
                    return TryGetRealFactoryReturnType(sd);
                }
                implType = ImplType.None;
                return null;
            }
#endif
            if (sd.ImplementationType != null)
            {
                implType = ImplType.Default;
                return sd.ImplementationType;
            }
            else if (sd.ImplementationInstance != null)
            {
                implType = ImplType.Instance;
                return sd.ImplementationInstance.GetType();
            }
            else if (sd.ImplementationFactory != null)
            {
                //var typeArguments = sd.ImplementationFactory.GetType().GenericTypeArguments;
                implType = ImplType.Factory;
                return TryGetRealFactoryReturnType(sd);
            }
            implType = ImplType.None;
            return null;
        }
    }

    private void AddServiceDescriptors(IServiceCollection serviceCollection,
#if NET8_0_OR_GREATER
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
#endif
        Type originType,
#if NET8_0_OR_GREATER
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
#endif
    Type proxyType, List<SDInfo> infos)
    {
        if (!infos.Any(s => s.ImplementationType == s.ServiceType && s.ImplementationType == originType))
        {
            // 没有自身的注入
            var sd = new ServiceDescriptor(originType, originType, ServiceLifetime.Scoped);
            var proxy = new ServiceDescriptor(proxyType, proxyType, ServiceLifetime.Scoped);
            serviceCollection.Add(sd);
            serviceCollection.Add(proxy);
        }
        foreach (var item in infos)
        {
            if (item.ImplementationType == item.ServiceType && item.ImplementationType == originType)
            {
                serviceCollection.Add(item.ServiceDescriptor);
                var proxy = new ServiceDescriptor(proxyType, proxyType, item.Lifetime);
                serviceCollection.Add(proxy);
                continue;
            }
            switch (item.ImplType)
            {
                case ImplType.Default:
                    HandleTypeRegistration(serviceCollection, item.ServiceDescriptor, proxyType);
                    break;
                case ImplType.Instance:
                    // 实例注册：直接注册
                    serviceCollection.Add(item.ServiceDescriptor);
                    break;
                case ImplType.Factory:
                    HandleFactoryRegistration(serviceCollection, item.ServiceDescriptor, proxyType);
                    break;
                default:
                    break;
            }
        }
    }

    private static void HandleTypeRegistration(IServiceCollection serviceCollection, ServiceDescriptor sd,
#if NET8_0_OR_GREATER
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
#endif
         Type proxyType)
    {
        // 注册代理类型
        var proxyServiceDescriptor = sd.IsKeyedService
            ? ServiceDescriptor.DescribeKeyed(sd.ServiceType, sd.ServiceKey, proxyType, sd.Lifetime)
            : ServiceDescriptor.Describe(sd.ServiceType, proxyType, sd.Lifetime);

        serviceCollection.TryAdd(proxyServiceDescriptor);
    }

    private void HandleFactoryRegistration(IServiceCollection serviceCollection, ServiceDescriptor sd,
#if NET8_0_OR_GREATER
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
#endif
        Type proxyType)
    {
        if (sd.IsKeyedService)
        {
            var keyedProxy = ServiceDescriptor.DescribeKeyed(proxyType,
                sd.ServiceKey,
                proxyType
                , sd.Lifetime);
            // 包装Keyed工厂
            var wrappedFactory = CreateKeyedFactory(proxyType);
            var factorySd = ServiceDescriptor.DescribeKeyed(
                sd.ServiceType,
                sd.ServiceKey,
                wrappedFactory,
                sd.Lifetime);
            serviceCollection.Add(keyedProxy);
            serviceCollection.Add(factorySd);
        }
        else
        {
            // 包装普通工厂
            var wrappedFactory = CreateFactory(proxyType);
            var factorySd = ServiceDescriptor.Describe(
                sd.ServiceType,
                wrappedFactory,
                sd.Lifetime);
            serviceCollection.Add(factorySd);
        }
    }

    private static readonly MethodInfo RKS = typeof(ServiceProviderKeyedServiceExtensions).GetMethod("GetRequiredKeyedService", [typeof(IServiceProvider), typeof(Type), typeof(object)])!;
    private static readonly MethodInfo RS = typeof(ServiceProviderServiceExtensions).GetMethod("GetRequiredService", [typeof(IServiceProvider), typeof(Type)])!;

    private readonly ConcurrentDictionary<Type, Func<IServiceProvider, object>> factoryCache = new();
    private readonly ConcurrentDictionary<Type, Func<IServiceProvider, object?, object>> keyedFactoryCache = new();

    private Func<IServiceProvider, object?, object> CreateKeyedFactory(Type proxyType)
    {
        return keyedFactoryCache.GetOrAdd(proxyType, k =>
        {
            var pe = Expression.Parameter(typeof(IServiceProvider), "provider");
            var ke = Expression.Parameter(typeof(object), "key");
            var proxyE = Expression.Constant(k, typeof(Type));
            var lambda = Expression.Lambda<Func<IServiceProvider, object?, object>>(Expression.Call(RKS, pe, proxyE, ke), pe, ke);
            return lambda.Compile();
        });
    }

    private Func<IServiceProvider, object> CreateFactory(Type proxyType)
    {
        return factoryCache.GetOrAdd(proxyType, k =>
        {
            var pe = Expression.Parameter(typeof(IServiceProvider), "provider");
            var proxyE = Expression.Constant(k, typeof(Type));
            var lambda = Expression.Lambda<Func<IServiceProvider, object>>(Expression.Call(RS, pe, proxyE), pe);
            return lambda.Compile();
        });
    }

    private Func<object, Type>? getAutoInjectFactoryRealReturnType;

#if NET8_0_OR_GREATER
    [RequiresUnreferencedCode("Uses reflection to load types and attributes.")]
#endif
    private Type? TryGetRealFactoryReturnType(ServiceDescriptor sd)
    {
        var realType = sd.GetType();
        if (realType.Name == "AutoInjectServiceDescriptor")
        {
            if (getAutoInjectFactoryRealReturnType is null)
            {
                var pe = Expression.Parameter(typeof(object), "sd");
                var lambda = Expression.Lambda<Func<object, Type>>(Expression.Property(Expression.Convert(pe, realType), "FactoryReturnType"), pe);
                getAutoInjectFactoryRealReturnType ??= lambda.Compile();
            }
            return getAutoInjectFactoryRealReturnType.Invoke(sd);
        }
        return DefalutGet(sd);
    }
    private static Type? DefalutGet(ServiceDescriptor sd)
    {
        var typeArguments = sd.ImplementationFactory?.GetType().GenericTypeArguments;
        return typeArguments?[1];
    }
}
