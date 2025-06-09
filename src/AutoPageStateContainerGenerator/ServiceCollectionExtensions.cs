using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

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
    /// <param name="config"></param>
    /// <returns></returns>
    public static IServiceCollection AddStateContainers(this IServiceCollection services, Action<StateContainerOption>? config = null)
    {
        var entry = Assembly.GetEntryAssembly();
        if (entry is null) return services;
        var refereaces = entry.GetReferencedAssemblies().Select(Assembly.Load);
        Assembly[] all = [entry, .. refereaces];

        var option = new StateContainerOption();
        config?.Invoke(option);

        foreach (var asm in all)
        {
            foreach (var type in asm.ExportedTypes)
            {
                var attributeData = type.GetCustomAttributesData().FirstOrDefault(a => a.AttributeType == typeof(GeneratedStateContainerAttribute));
                if (attributeData == null)
                    continue;
                //services.AddScoped(type);
                //var lifetime = attr.Lifetime.HasValue ? (ServiceLifetime)attr.Lifetime.Value : option.InjectLifetime;
                var lifetimeArgument = attributeData.NamedArguments
            .FirstOrDefault(arg => arg.MemberName == "Lifetime");
                if (lifetimeArgument != default)
                {
                    var lifetime = (ServiceLifetime)(int)lifetimeArgument.TypedValue.Value!;
                    services.Add(new ServiceDescriptor(type, type, lifetime));
                }
                else
                {
                    services.Add(new ServiceDescriptor(type, type, option.InjectLifetime));
                }
                //var lifetime = attr.Lifetime > -1 ? (ServiceLifetime)attr.Lifetime : option.InjectLifetime;
                //services.Add(new ServiceDescriptor(type, type, lifetime));
                //Debug.WriteLine($"{type.FullName} -> {lifetime}");
            }
        }
        return services;
    }
}
