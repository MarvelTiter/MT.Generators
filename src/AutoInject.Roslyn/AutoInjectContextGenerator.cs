﻿using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Text;
using Generators.Shared;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Diagnostics;
using System.Linq;
using Generators.Shared.Builder;
using Microsoft.CodeAnalysis.CSharp;

namespace AutoInjectGenerator
{
    [Generator(LanguageNames.CSharp)]
    public class AutoInjectContextGenerator : IIncrementalGenerator
    {
        const string AutoInjectContext = "AutoInjectGenerator.AutoInjectContextAttribute";
        const string AutoInjectConfiguration = "AutoInjectGenerator.AutoInjectConfiguration";
        const string AutoInject = "AutoInjectGenerator.AutoInjectAttribute";
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var source = context.SyntaxProvider.ForAttributeWithMetadataName(
                AutoInjectContext
                , static (node, _) => node is ClassDeclarationSyntax
                , static (ctx, _) => ctx);
            context.RegisterSourceOutput(source, (context, source) =>
            {
                var compilcation = source.SemanticModel.Compilation;
                var used = compilcation.GetUsedAssemblyReferences();
                var all = GetAllSymbols(compilcation.GlobalNamespace);
                var classSymbol = (INamedTypeSymbol)source.TargetSymbol;
                var className = classSymbol.MetadataName;
                if (classSymbol.GetMembers().FirstOrDefault(m => m is IMethodSymbol { IsPartialDefinition: true, IsStatic: true }) is not IMethodSymbol methodSymbol)
                {
                    context.ReportDiagnostic(DiagnosticDefinitions.AIG00001(source.TargetNode.GetLocation()));
                    return;
                }
                // 获取 IServiceCollection 参数的名称
                var serviceName = methodSymbol.Parameters.First(p => p.Type.ToDisplayString().Contains("Microsoft.Extensions.DependencyInjection.IServiceCollection")).Name;

                var allConfig = methodSymbol.GetAttributes(AutoInjectConfiguration).Select(c =>
                {
                    var i = c.GetNamedValue("Include")?.ToString() ?? "";
                    var e = c.GetNamedValue("Exclude")?.ToString() ?? "";
                    return (i, e);
                });

                var includes = allConfig.Select(t => t.i).Where(s => !string.IsNullOrEmpty(s)).ToArray();
                var excludes = allConfig.Select(t => t.e).Where(s => !string.IsNullOrEmpty(s)).ToArray();

                if (includes.Intersect(excludes).Any())
                {
                    context.ReportDiagnostic(DiagnosticDefinitions.AIG00002(source.TargetNode.GetLocation()));
                    return;
                }

                var injectStatements = CreateInjectStatements(all, serviceName, includes, excludes);
                var cm = MethodBuilder.Default.MethodName(methodSymbol.Name)
                    .Modifiers("public static partial")
                    .AddParameter([.. methodSymbol.Parameters.Select((p, i) => $"{(i == 0 && methodSymbol.IsExtensionMethod ? "this " : "")}{p.Type.ToDisplayString()} {p.Name}")])
                    .AddBody([.. injectStatements]);

                var gclass = ClassBuilder.Default.ClassName(className)
                    .Modifiers("static partial")
                    .AddMembers(cm);
                var gn = NamespaceBuilder.Default.Namespace(source.TargetSymbol.ContainingNamespace.ToDisplayString())
                    .AddMembers(gclass);

                var file = CodeFile.New($"{className}.AutoInject.g.cs")
                      .AddMembers(gn)
                      .AddUsings(source.GetTargetUsings());

                context.AddSource(file);
            });
        }

        private static IEnumerable<Statement> CreateInjectStatements(IEnumerable<INamedTypeSymbol> all
            , string serviceName
            , string[] includes
            , string[] excludes)
        {
            foreach (var c in all)
            {
                foreach (var a in c.GetAttributes(AutoInject))
                {
                    // 没有配置规则，全部注入
                    if (includes.Length > 0 || excludes.Length > 0)
                    {
                        if (a.GetNamedValue("Group", out var group))
                        {
                            /*
                             * 配置了Group属性
                             * 1. 不在excludes中才能注册
                             * 2. 存在includes中才能注册
                             */
                            if (excludes.Contains(group) || !includes.Contains(group))
                            {
                                continue;
                            }
                        }
                    }
                    var implType = c.ToDisplayString();
                    var serviceType = "";
                    if (a.GetNamedValue("ServiceType", out var t) && t is INamedTypeSymbol type)
                    {
                        serviceType = type.ToDisplayString();
                    }
                    // 获取到的Interfaces跟AllInterfaces一样
                    else if (c.Interfaces.Length >= 1)
                    {
                        serviceType = c.Interfaces.First().ToDisplayString();
                    }
                    else
                    {
                        serviceType = implType;
                    }

                    if (!a.GetNamedValue("LifeTime", out var injectType))
                    {
                        // AutoInjectGenerator.InjectLifeTime
                        injectType = 1;
                    }
                    if (serviceType == implType)
                    {
                        yield return $"{serviceName}.Add{FormatInjectType(injectType)}<{implType}>()";
                    }
                    else
                    {
                        yield return $"{serviceName}.Add{FormatInjectType(injectType)}<{serviceType}, {implType}>()";
                    }
                }
            }
        }

        private static string FormatInjectType(object? t)
        {
            return t switch
            {
                0 => "Transient",
                1 => "Scoped",
                2 => "Singleton",
                _ => "Scoped"
            };
        }

        private static IEnumerable<INamedTypeSymbol> GetAllSymbols(INamespaceSymbol global)
        {
            foreach (var symbol in global.GetMembers())
            {
                if (symbol is INamespaceSymbol s)
                {
                    foreach (var item in GetAllSymbols(s))
                    {
                        //if (item.HasAttribute(AutoInject))
                        yield return item;
                    }
                }
                else if (symbol is INamedTypeSymbol target && !target.IsImplicitlyDeclared)
                {
                    if (target.HasAttribute(AutoInject))
                        yield return target;
                }
            }
        }
    }
}
