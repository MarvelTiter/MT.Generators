﻿using Generators.Shared;
using Generators.Shared.Builder;
using Microsoft.CodeAnalysis;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;

namespace AutoInjectGenerator;

internal static class AutoInjectContextGeneratorHelpers
{
    public const string AutoInjectContext = "AutoInjectGenerator.AutoInjectContextAttribute";
    public const string AutoInjectConfiguration = "AutoInjectGenerator.AutoInjectConfiguration";
    public const string AutoInject = "AutoInjectGenerator.AutoInjectAttribute";
    public const string AutoInjectSelf = "AutoInjectGenerator.AutoInjectSelfAttribute";
    private static bool IsInjectSelf(AttributeData data) => data.AttributeClass?.ToDisplayString() == AutoInjectSelf;

    // new ServiceDescriptor()
    private static string FormatInjectType(object? t)
    {
        return t switch
        {
            0 => "ServiceLifetime.Singleton",
            1 => "ServiceLifetime.Scoped",
            2 => "ServiceLifetime.Transient",
            _ => "ServiceLifetime.Scoped"
        };
    }

    public static AutoInjectContextInfo CollectContextInfo(GeneratorAttributeSyntaxContext context, CancellationToken _)
    {
        var classSymbol = (INamedTypeSymbol)context.TargetSymbol;
        var ctxInfo = new AutoInjectContextInfo(classSymbol);
        if (classSymbol.GetMembers().FirstOrDefault(m => m is IMethodSymbol
            {
                IsPartialDefinition: true, PartialImplementationPart: null, IsStatic: true
            }) is not IMethodSymbol methodSymbol)
        {
            ctxInfo.Diagnostic = DiagnosticDefinitions.AIG00001(context.TargetNode.GetLocation());
            return ctxInfo;
        }
        ctxInfo.MethodSymbol = methodSymbol;
        var allConfig = methodSymbol.GetAttributes(AutoInjectConfiguration).Select(c =>
        {
            var i = c.GetNamedValue("Include")?.ToString() ?? "";
            var e = c.GetNamedValue("Exclude")?.ToString() ?? "";
            return (i, e);
        }).ToArray();

        ctxInfo.Includes = [.. allConfig.Select(t => t.i).Where(s => !string.IsNullOrEmpty(s))];

        ctxInfo.Excludes = [.. allConfig.Select(t => t.e).Where(s => !string.IsNullOrEmpty(s))];
        if (ctxInfo.Includes.Intersect(ctxInfo.Excludes).Any())
        {
            var methodLocation = methodSymbol.TryGetLocation();
            ctxInfo.Diagnostic = DiagnosticDefinitions.AIG00002(methodLocation);
            return ctxInfo;
        }
        return ctxInfo;
    }

    public static AutoInjectContextInfo CollectContextInfo(GeneratorSyntaxCollectInfoContext context)
    {
        var classSymbol = (INamedTypeSymbol)context.TargetSymbol;
        var ctxInfo = new AutoInjectContextInfo(classSymbol);
        if (classSymbol.GetMembers().FirstOrDefault(m => m is IMethodSymbol
            {
                IsPartialDefinition: true, PartialImplementationPart: null, IsStatic: true
            }) is not IMethodSymbol methodSymbol)
        {
            ctxInfo.Diagnostic = DiagnosticDefinitions.AIG00001(context.GetDiagnosticLocation());
            return ctxInfo;
        }
        ctxInfo.MethodSymbol = methodSymbol;
        var allConfig = methodSymbol.GetAttributes(AutoInjectConfiguration).Select(c =>
        {
            var i = c.GetNamedValue("Include")?.ToString() ?? "";
            var e = c.GetNamedValue("Exclude")?.ToString() ?? "";
            return (i, e);
        }).ToArray();

        ctxInfo.Includes = [.. allConfig.Select(t => t.i).Where(s => !string.IsNullOrEmpty(s))];

        ctxInfo.Excludes = [.. allConfig.Select(t => t.e).Where(s => !string.IsNullOrEmpty(s))];
        if (ctxInfo.Includes.Intersect(ctxInfo.Excludes).Any())
        {
            var methodLocation = methodSymbol.TryGetLocation();
            ctxInfo.Diagnostic = DiagnosticDefinitions.AIG00002(methodLocation);
            return ctxInfo;
        }
        return ctxInfo;
    }

    public static AutoInjectInfo CollectInjectInfo(GeneratorSyntaxCollectInfoContext context)
    {
        var classSymbol = (INamedTypeSymbol)context.TargetSymbol;
        var info = new AutoInjectInfo(classSymbol);
        foreach (var a in classSymbol.GetAttributes(AutoInject, true))
        {
            if (!a.GetNamedValue("LifeTime", out var injectType))
            {
                // AutoInjectGenerator.InjectLifeTime
                injectType = 1;
            }
            var scoped = FormatInjectType(injectType);
            info.Scoped ??= scoped;
            if (info.Scoped != scoped)
            {
                // 有不同的生命周期
                info.Diagnostic = DiagnosticDefinitions.AIG00004(context.GetDiagnosticLocation());
                return info;
            }
            string serviceType;
            //var services = classSymbol.GetInterfaces().ToArray();
            if (IsInjectSelf(a))
            {
                serviceType = info.Implement;
            }
            else if (a.GetNamedValue("ServiceType", out var t) && t is INamedTypeSymbol type)
            {
                serviceType = type.ToDisplayString();
                if (!classSymbol.AllInterfaces.Contains(type)
                    && !SymbolEqualityComparer.Default.Equals(classSymbol, type)
                    && !classSymbol.IsSubClassOf(type))
                {
                    info.Diagnostic = DiagnosticDefinitions.AIG00003(serviceType, info.Implement, context.GetDiagnosticLocation());
                    return info;
                }
            }
            // 获取到的Interfaces跟AllInterfaces一样
            else if (classSymbol.Interfaces.Length >= 1)
            {
                serviceType = classSymbol.Interfaces[0].ToDisplayString();
            }
            else
            {
                serviceType = info.Implement;
            }

            _ = a.GetNamedValue<string>("ServiceKey", out var serviceKey);
            if (a.GetNamedValue<string>("Group", out var group))
            {
                info.MemberShip ??= group;
                if (info.MemberShip != group)
                {
                    // 有不同的Group
                    info.Diagnostic = DiagnosticDefinitions.AIG00004(context.GetDiagnosticLocation());
                    return info;
                }
            }
            //_ = a.GetNamedValue<bool>("IsTry", out var tryAdd);
            info.Services.Add(new RegisterServiceInfo(serviceType, serviceKey));
        }
        return info;
    }

    public static CodeFile? CreateContextCodeFile(AutoInjectContextInfo context, IEnumerable<AutoInjectInfo?> items)
    {
        var classSymbol = context.TargetSymbol;
        var className = context.ClassName;
        var methodSymbol = context.MethodSymbol!;
        var gn = NamespaceBuilder.Default.Namespace(classSymbol.ContainingNamespace.ToDisplayString());
        var gclass = ClassBuilder.Default
            .ClassName(className)
            .AddGeneratedCodeAttribute(typeof(AutoInjectContextGenerator))
            .Modifiers("static partial");

        var serviceName = methodSymbol.Parameters.First(p =>
            p.Type.ToDisplayString().Contains("Microsoft.Extensions.DependencyInjection.IServiceCollection")).Name;

        var cm = MethodBuilder.Default.MethodName(methodSymbol.Name)
            .Modifiers("public static partial")
            .AddGeneratedCodeAttribute(typeof(AutoInjectContextGenerator))
            .AddParameter([
                .. methodSymbol.Parameters.Select((p, i) =>
                    $"{(i == 0 && methodSymbol.IsExtensionMethod ? "this " : "")}{p.Type.ToDisplayString()} {p.Name}")
            ]);

        foreach (var item in items)
        {
            cm.AddBody([.. CreateRegisterStatement(context, serviceName, item)]);
        }
        gclass.AddMembers(cm);

        var file = CodeFile.New($"{className}.AutoInject.g.cs")
            .AddUsings("using Microsoft.Extensions.DependencyInjection;")
            .AddUsings("using Microsoft.Extensions.DependencyInjection.Extensions;")
            .AddUsings("using AutoInjectGenerator.Models;")
            .AddMembers(gn.AddMembers(gclass));

#if DEBUG
        var ss = file.ToString();
#endif
        return file;
    }

    //const string SD = "global::Microsoft.Extensions.DependencyInjection.ServiceDescriptor";
    const string SD = "AutoInjectServiceDescriptor";
    private static IEnumerable<string> CreateRegisterStatement(AutoInjectContextInfo context, string serviceName, AutoInjectInfo? item)
    {
        if (item is null || item.Services.Count == 0)
        {
            yield return "// No services to register";
            yield break;
        }
        if (!ShouldRegister(context, item.MemberShip))
        {
            yield return $"// Skipped registering {item.Implement} due to configuration.";
            yield break;
        }
        yield return $"// {item.Implement} 类型的相关注册";
        string implement = $"typeof({item.Implement})";

        //foreach (var r in item.Services)
        //{
        //    yield return DoCreate(context, serviceName, r.ServiceType, implement, item.Scoped!, r.Key);
        //}

        if (item.Services.Count == 1)
        {
            var r = item.Services[0];
            yield return DoCreate(context, serviceName, r.ServiceType, implement, item.Scoped!, r.Key);
        }
        else
        {
            // 多次注入同一个服务，但是没有注入自身
            var self = item.Services.FirstOrDefault(s => s.ServiceType == item.Implement);
            if (self is null)
            {
                // 提供默认的自身注入
                yield return DoCreate(context, serviceName, item.Implement, implement, item.Scoped!, null, true);
            }
            foreach (var r in item.Services)
            {
                if (r.ServiceType == item.Implement)
                {
                    // 显式注入自身，说明前面的self不为null
                    yield return DoCreate(context, serviceName, r.ServiceType, implement, item.Scoped!, r.Key, true);
                }
                else
                {
                    var factoryExpression = $"p => p.GetRequiredService<{item.Implement}>()";
                    if (r.Key is not null)
                    {
                        factoryExpression = $"(p, k) => p.GetRequiredKeyedService<{item.Implement}>(k)";
                        if (self is null || r.Key != self.Key)
                        {
                            // 注入自身
                            yield return DoCreate(context, serviceName, item.Implement, implement, item.Scoped!, r.Key);
                        }
                    }
                    // 注入接口
                    yield return DoCreate(context, serviceName, r.ServiceType, factoryExpression, item.Scoped!, r.Key, factoryReturnType: $", {implement}");
                }
            }
        }

        yield return "";
        static bool ShouldRegister(AutoInjectContextInfo context, string? group)
        {
            if (group is null)
            {
                return true;
            }
            // 没有配置规则，全部注入
            if (context.Includes?.Length == 0 && context.Excludes?.Length == 0)
            {
                return true;
            }
            /*
             * 配置了Group属性
             * 1. 不在excludes中才能注册
             * 2. 存在includes中才能注册
             */
            if (context.Excludes.Contains(group) || !context.Includes.Contains(group))
            {
                return false;
            }
            return true;
        }

        static string DoCreate(AutoInjectContextInfo context, string serviceName, string serviceType, string implement, string scoped, string? key, bool tryadd = false, string? factoryReturnType = "")
        {
            var method = tryadd ? "TryAdd" : "Add";
            if (key is not null)
            {
                return $"""{serviceName}.{method}(new {SD}(typeof({serviceType}),"{key}", {implement}, {scoped}{factoryReturnType}))""";
            }
            else
            {
                return $"{serviceName}.{method}(new {SD}(typeof({serviceType}), {implement}, {scoped}{factoryReturnType}))";
            }
        }
    }
}