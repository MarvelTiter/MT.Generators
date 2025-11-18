using AutoWasmApiGenerator.Attributes;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
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

    /// <summary>
    /// 注入生成器生成的API调用类
    /// </summary>
    /// <param name="services"></param>
    /// <param name="overrideLifetime"></param>
    /// <returns></returns>
#if NET8_0_OR_GREATER
    [RequiresUnreferencedCode("Uses reflection to load types and attributes.")]
#endif
    public static IServiceCollection AddGeneratedApiInvokerServices(this IServiceCollection services
        , Func<Type, ServiceLifetime>? overrideLifetime = null)
    {
        var entry = Assembly.GetEntryAssembly();
        if (entry is null) return services;
        //var refereaces = entry.GetReferencedAssemblies().Select(Assembly.Load);
        //Assembly[] all = [entry, .. refereaces];
        var all = GetAllReferencedAssemblies(entry).ToArray();

        foreach (var asm in all)
        {
            foreach (var type in asm.ExportedTypes)
            {
                var attributeData = type.GetCustomAttributesData().FirstOrDefault(a => a.AttributeType == typeof(GeneratedByAutoWasmApiGeneratorAttribute));
                if (attributeData == null)
                    continue;
                var partArgument = attributeData.NamedArguments.FirstOrDefault(arg => arg.MemberName == nameof(GeneratedByAutoWasmApiGeneratorAttribute.Part));
                if (partArgument.TypedValue.Value is 1)
                {
                    var interfaceType = attributeData.NamedArguments.FirstOrDefault(arg => arg.MemberName == nameof(GeneratedByAutoWasmApiGeneratorAttribute.InterfaceType)).TypedValue.Value as Type;
                    var lifetime = overrideLifetime?.Invoke(interfaceType!) ?? ServiceLifetime.Scoped;
                    services.Add(new ServiceDescriptor(interfaceType!, type, lifetime));
                }

            }
        }
        return services;
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
