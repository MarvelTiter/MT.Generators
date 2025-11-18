using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace AutoPageStateContainerGenerator;

/// <summary>
/// 
/// </summary>
public class StateContainerOption
{
    /// <summary>
    /// 全局配置，优先级低于<see cref="StateContainerAttribute.Lifetime"/>
    /// </summary>
    public ServiceLifetime InjectLifetime { get; set; } = ServiceLifetime.Scoped;
}

/// <summary>
/// 
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddStateContainerManager(this IServiceCollection services)
    {
        services.AddScoped<IStateContainerManager, DefaultStateContainer>();
        return services;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="services"></param>
    /// <param name="config"></param>
    /// <returns></returns>
#if NET8_0_OR_GREATER
    [RequiresUnreferencedCode("Uses reflection to load types and attributes.")]
#endif
    public static IServiceCollection AddStateContainers(this IServiceCollection services, Action<StateContainerOption>? config = null)
    {
        var entry = Assembly.GetEntryAssembly();
        if (entry is null) return services;
        //var refereaces = entry.GetReferencedAssemblies().Select(Assembly.Load);
        //Assembly[] all = [entry, .. refereaces];
        var all = GetAllReferencedAssemblies(entry).ToArray();

        services.AddScoped<IStateContainerManager, DefaultStateContainer>();

        var option = new StateContainerOption();
        config?.Invoke(option);

        foreach (var asm in all)
        {
            foreach (var type in asm.ExportedTypes)
            {
                var attributeData = type.GetCustomAttributesData().FirstOrDefault(a => a.AttributeType == typeof(GeneratedStateContainerAttribute));
                if (attributeData == null)
                    continue;
                var lifetimeArgument = attributeData.NamedArguments.FirstOrDefault(arg => arg.MemberName == nameof(StateContainerAttribute.Lifetime));
                var interfaceType = attributeData.NamedArguments.FirstOrDefault(arg => arg.MemberName == nameof(StateContainerAttribute.Implements)).TypedValue.Value as Type;
                var lifetime = lifetimeArgument != default ?
                    (ServiceLifetime)(int)lifetimeArgument.TypedValue.Value!
                    : option.InjectLifetime;
                var sd = new ServiceDescriptor(type, type, lifetime);
                services.Add(sd);
                if (interfaceType is not null)
                {
                    var factory = CreateFactory(type);
                    var isd = new ServiceDescriptor(interfaceType, factory, lifetime);
                    services.Add(isd);
                }

                var namedArgument = attributeData.NamedArguments.FirstOrDefault(arg => arg.MemberName == nameof(StateContainerAttribute.Name));
                if (namedArgument != default)
                {
                    DefaultStateContainer.Add($"{namedArgument.TypedValue.Value}", type);
                }
                //if (interfaceType is not null)
                //{
                //    DefaultStateContainer.Add(namedArgument.TypedValue.Value?.ToString(), type, interfaceType);
                //}
            }
        }
        return services;
    }

    private static readonly MethodInfo RS = typeof(ServiceProviderServiceExtensions).GetMethod("GetRequiredService", [typeof(IServiceProvider), typeof(Type)])!;

    private static Func<IServiceProvider, object> CreateFactory(Type proxyType)
    {
        var pe = Expression.Parameter(typeof(IServiceProvider), "provider");
        var proxyE = Expression.Constant(proxyType, typeof(Type));
        var lambda = Expression.Lambda<Func<IServiceProvider, object>>(Expression.Call(RS, pe, proxyE),pe);
        return lambda.Compile();
    }
#if NET8_0_OR_GREATER
    [RequiresUnreferencedCode("Uses reflection to load types and attributes.")]
#endif
    private static IEnumerable<Assembly> GetAllReferencedAssemblies(Assembly entryAssembly)
    {
        var visited = new HashSet<string>();
        var stack = new Stack<Assembly>();

        stack.Push(entryAssembly);
        visited.Add(entryAssembly.FullName!);

        while (stack.Count > 0)
        {
            var assembly = stack.Pop();
            yield return assembly;

            foreach (var reference in assembly.GetReferencedAssemblies())
            {
                if (!visited.Add(reference.FullName))
                    continue;

                try
                {
                    var referencedAssembly = Assembly.Load(reference);
                    stack.Push(referencedAssembly);
                }
                catch
                {
                    // 忽略加载失败的程序集
                }
            }
        }
    }
}
