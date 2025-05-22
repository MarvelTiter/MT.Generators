using Microsoft.CodeAnalysis;
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

namespace AutoInjectGenerator;

public static class Ex
{
    /// <summary>
    /// symbol 是否可以作为 other的子类
    /// </summary>
    /// <param name="symbol"></param>
    /// <param name="other"></param>
    /// <returns></returns>
    public static bool IsSubClassOf(this INamedTypeSymbol symbol, INamedTypeSymbol other)
    {
        if (SymbolEqualityComparer.Default.Equals(symbol.BaseType, other))
        {
            return true;
        }
        else
        {
            if (symbol.BaseType is null)
            {
                return false;
            }
            return symbol.BaseType.IsSubClassOf(other);
        }
    }
}

[Generator(LanguageNames.CSharp)]
public class AutoInjectContextGenerator : IIncrementalGenerator
{
    const string AutoInjectContext = "AutoInjectGenerator.AutoInjectContextAttribute";
    const string AutoInjectConfiguration = "AutoInjectGenerator.AutoInjectConfiguration";
    const string AutoInject = "AutoInjectGenerator.AutoInjectAttribute";
    const string AutoInjectSelf = "AutoInjectGenerator.AutoInjectSelfAttribute";

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
                var all = source.GetAllSymbols(AutoInject, true);
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

        //var injectStatements = CreateInjectStatements(all, serviceName, includes, excludes, context);
        var allItems = CollectAll(all, includes, excludes);
        var errors = allItems.Where(i => i.TypeError);
        if (errors.Any())
        {
            foreach (var item in errors)
            {
                context.ReportDiagnostic(DiagnosticDefinitions.AIG00003(item.Service, item.Implement, item.ClassSymbol.Locations.FirstOrDefault()));
            }
        }
        var injectStatements = CreateInjectStatements(serviceName, allItems);
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
    struct InjectItem
    {
        public INamedTypeSymbol ClassSymbol { get; set; }
        public string InjectMethod { get; set; }
        public string Service { get; set; }
        public string Implement { get; set; }
        public string? Key { get; set; }
        public bool TypeError { get; set; }
    }
    private static List<InjectItem> CollectAll(IEnumerable<INamedTypeSymbol> all
        , IReadOnlyCollection<string> includes
        , IReadOnlyCollection<string> excludes)
    {
        List<InjectItem> items = [];
        foreach (var classSymbol in all)
        {
            foreach (var a in classSymbol.GetAttributes(AutoInject, true))
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

                var implType = classSymbol.ToDisplayString();
                var typeError = false;
                string? serviceType;
                //var services = classSymbol.GetInterfaces().ToArray();
                if (IsInjectSelf(a))
                {
                    serviceType = implType;
                }
                else if (a.GetNamedValue("ServiceType", out var t) && t is INamedTypeSymbol type)
                {
                    serviceType = type.ToDisplayString();
                    if (!classSymbol.AllInterfaces.Contains(type)
                        && !SymbolEqualityComparer.Default.Equals(classSymbol, type)
                        && !classSymbol.IsSubClassOf(type))
                    {
                        typeError = true;
                    }
                }
                // 获取到的Interfaces跟AllInterfaces一样
                else if (classSymbol.Interfaces.Length >= 1)
                {
                    serviceType = classSymbol.Interfaces[0].ToDisplayString();
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
                items.Add(new()
                {
                    ClassSymbol = classSymbol,
                    InjectMethod = method,
                    Service = serviceType,
                    Implement = implType,
                    Key = key,
                    TypeError = typeError
                });
            }
        }
        return items;
    }

    private static bool IsInjectSelf(AttributeData data) => data.AttributeClass?.ToDisplayString() == AutoInjectSelf;

    private static IEnumerable<Statement> CreateInjectStatements(string serviceName, List<InjectItem> items)
    {
        foreach (var item in items)
        {
            if (item.TypeError) continue;
            if (item.Service == item.Implement)
            {
                yield return $"{serviceName}.{item.InjectMethod}<{item.Implement}>({item.Key})";
            }
            else
            {
                yield return $"{serviceName}.{item.InjectMethod}<{item.Service}, {item.Implement}>({item.Key})";
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