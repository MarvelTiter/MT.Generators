using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
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
    /// 
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
                if (type.GetCustomAttribute<GeneratedStateContainerAttribute>() == null)
                    continue;
                //services.AddScoped(type);
                services.Add(new ServiceDescriptor(type, type, option.InjectLifetime));
            }
        }
        return services;
    }
}
