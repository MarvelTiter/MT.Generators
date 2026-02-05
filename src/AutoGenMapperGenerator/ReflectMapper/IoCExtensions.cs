using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace AutoGenMapperGenerator.ReflectMapper;

/// <summary>
/// 
/// </summary>
public static class IoCExtensions
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="services"></param>
    /// <param name="optionsAction"></param>
    /// <returns></returns>
    public static IServiceCollection AddMapperService(this  IServiceCollection services, Action<MapperOptions>? optionsAction = null)
    {
        optionsAction?.Invoke(MapperOptions.Instance);
        services.AddSingleton(MapperOptions.Instance);
        services.AddSingleton<IMapperService, MapperService>();
        return services;
    }
}
