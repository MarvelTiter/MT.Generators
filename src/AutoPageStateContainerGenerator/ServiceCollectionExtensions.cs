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
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddStateContainers(this IServiceCollection services)
    {
        var entry = Assembly.GetEntryAssembly();
        if (entry is null) return services;
        var refereaces = entry.GetReferencedAssemblies().Select(Assembly.Load);
        Assembly[] all = [entry, .. refereaces];
        foreach (var asm in all)
        {
            foreach (var type in asm.ExportedTypes)
            {
                if (type.GetCustomAttribute<GeneratedStateContainerAttribute>() == null)
                    continue;
                services.AddScoped(type);
            }
        }
        return services;
    }
}
