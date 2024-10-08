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
using System.Threading;

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
            //只能获取到当前程序集的节点
            //var ctx = context.SyntaxProvider.ForAttributeWithMetadataName(
            //    AutoInjectContext
            //    , static (node, _) => node is ClassDeclarationSyntax
            //    , static (ctx, _) => ctx);
            //var services = context.SyntaxProvider.ForAttributeWithMetadataName(
            //    AutoInject
            //    , static (node, _) => node is ClassDeclarationSyntax
            //    , static (ctx, _) => ctx);
            //context.RegisterSourceOutput(ctx.Combine(services.Collect()), static (context, source) =>
            //{
            //    var injectContext = (INamedTypeSymbol)source.Left.TargetSymbol;
            //    var all = source.Right.Select(s => s.TargetSymbol).Cast<INamedTypeSymbol>();
            //    CreateContextFile(context, injectContext, all);
            //});
            context.RegisterSourceOutput(context.CompilationProvider, static (context, source) =>
            {

                var allContext = source.GetAllSymbols(AutoInjectContext).ToArray();
                if (allContext.Length == 0)
                {
                    return;
                }

                foreach (var item in allContext)
                {
                    if (!EqualityComparer<IAssemblySymbol>.Default.Equals(item.ContainingAssembly, source.SourceModule.ContainingAssembly))
                    {
                        continue;
                    }
                    var all = source.GetAllSymbols(AutoInject);
                    CreateContextFile(context, item, all);
                }
            });
        }

        private static void CreateContextFile(SourceProductionContext context, INamedTypeSymbol classSymbol,
            IEnumerable<INamedTypeSymbol> all)
        {
            var className = classSymbol.MetadataName;
            var methodSymbol = classSymbol.GetMembers().FirstOrDefault(m => m is IMethodSymbol
            {
                IsPartialDefinition: true, IsStatic: true
            }) as IMethodSymbol;
            if (methodSymbol == null)
            {
                context.ReportDiagnostic(DiagnosticDefinitions.AIG00001(Location.None));
                return;
            }

            var gn = NamespaceBuilder.Default.Namespace(classSymbol.ContainingNamespace.ToDisplayString());
            var gclass = ClassBuilder.Default.ClassName(className).Modifiers("static partial");

            // 获取 IServiceCollection 参数的名称
            var serviceName = methodSymbol.Parameters.First(p =>
                p.Type.ToDisplayString().Contains("Microsoft.Extensions.DependencyInjection.IServiceCollection")).Name;

            var allConfig = methodSymbol.GetAttributes(AutoInjectConfiguration).Select(c =>
            {
                var i = c.GetNamedValue("Include")?.ToString() ?? "";
                var e = c.GetNamedValue("Exclude")?.ToString() ?? "";
                return (i, e);
            }).ToArray();

            var includes = allConfig.Select(t => t.i).Where(s => !string.IsNullOrEmpty(s)).ToArray();
            var excludes = allConfig.Select(t => t.e).Where(s => !string.IsNullOrEmpty(s)).ToArray();

            if (includes.Intersect(excludes).Any())
            {
                context.ReportDiagnostic(DiagnosticDefinitions.AIG00002(Location.None));
                return;
            }

            var injectStatements = CreateInjectStatements(all, serviceName, includes, excludes);
            var cm = MethodBuilder.Default.MethodName(methodSymbol.Name)
                .Modifiers("public static partial")
                .AddParameter([
                    .. methodSymbol.Parameters.Select((p, i) =>
                        $"{(i == 0 && methodSymbol.IsExtensionMethod ? "this " : "")}{p.Type.ToDisplayString()} {p.Name}")
                ])
                .AddBody([.. injectStatements]);

            gclass.AddMembers(cm);


            var file = CodeFile.New($"{className}.AutoInject.g.cs")
                .AddUsings("using Microsoft.Extensions.DependencyInjection.Extensions;")
                .AddMembers(gn.AddMembers(gclass));
#if DEBUG
            var ss = file.ToString();
#endif
            context.AddSource(file);
        }

        private static IEnumerable<Statement> CreateInjectStatements(IEnumerable<INamedTypeSymbol> all
            , string serviceName
            , IReadOnlyCollection<string> includes
            , IReadOnlyCollection<string> excludes)
        {
            foreach (var c in all)
            {
                foreach (var a in c.GetAttributes(AutoInject))
                {
                    // 没有配置规则，全部注入
                    if (includes.Count > 0 || excludes.Count > 0)
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

                    _ = a.GetNamedValue<string>("ServiceKey", out var serviceKey);

                    _ = a.GetNamedValue<bool>("IsTry", out var tryAdd);
                    var method = AddMethodName(serviceKey, tryAdd, injectType, out var key);
                    if (serviceType == implType)
                    {
                        yield return $"{serviceName}.{method}<{implType}>({key})";
                    }
                    else
                    {
                        yield return $"{serviceName}.{method}<{serviceType}, {implType}>({key})";
                    }
                }
            }
        }

        private static string AddMethodName(string? serviceKey, bool tryAdd, object? injectType, out string key)
        {
            key = string.IsNullOrEmpty(serviceKey) ? string.Empty : $"\"{serviceKey}\"";
            var lifetime = FormatInjectType(injectType);
            return $"{(tryAdd ? "Try" : "")}Add{(string.IsNullOrEmpty(serviceKey) ? string.Empty : "Keyed")}{lifetime}";
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
    }
}

//namespace AutoInjectGenerator
//{
//    [Generator(LanguageNames.CSharp)]
//    public class AutoInjectContextGenerator : IIncrementalGenerator
//    {
//        const string AutoInjectContext = "AutoInjectGenerator.AutoInjectContextAttribute";
//        const string AutoInjectConfiguration = "AutoInjectGenerator.AutoInjectConfiguration";
//        const string AutoInject = "AutoInjectGenerator.AutoInjectAttribute";
//        public void Initialize(IncrementalGeneratorInitializationContext context)
//        {
//            var source = context.SyntaxProvider.ForAttributeWithMetadataName(
//                AutoInjectContext
//                , static (node, _) => node is ClassDeclarationSyntax
//                , static (ctx, _) => ctx);
//            context.RegisterSourceOutput(source, (context, source) =>
//            {
//                //var compilcation = source.SemanticModel.Compilation;
//                //var used = compilcation.GetUsedAssemblyReferences();

//                //var allContext = source.GetAllSymbols(AutoInjectContext);
//                //if (allContext.Any() == false)
//                //{
//                //    return;
//                //}

//                //foreach (var item in allContext)
//                //{
//                //    if (!EqualityComparer<IAssemblySymbol>.Default.Equals(item.ContainingAssembly, source.SourceModule.ContainingAssembly))
//                //    {
//                //        continue;
//                //    }
//                //    var all = source.GetAllSymbols(AutoInject);
//                //    CreateContextFile(context, item, all);
//                //}

//                var all = source.SemanticModel.Compilation.GetAllSymbols(AutoInject);
//                CreateContextFile(context, (INamedTypeSymbol)source.TargetSymbol, all);

//            });
//        }


//    }
//}