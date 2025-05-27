using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace AutoWasmApiGenerator;

/// <summary>
/// 
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 自定义异常返回值
    /// </summary>
    /// <param name="services"></param>
    /// <param name="action"></param>
    /// <returns></returns>
    public static IServiceCollection AddAutoWasmErrorResultHandler(this IServiceCollection services, Action<IExceptionResultConfigurator> action)
    {
        var i = new ExceptionResultFactory();
        action.Invoke(i);
        services.AddSingleton<IExceptionResultFactory>(i);
        return services;
    }
}
