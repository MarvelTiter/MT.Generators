using Generators.Shared;
using Generators.Shared.Builder;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using static AutoInjectGenerator.AutoInjectContextGeneratorHelpers;
namespace AutoInjectGenerator;

[Generator(LanguageNames.CSharp)]
public class AutoInjectModuleGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        //var vvv = context.SyntaxProvider.ForAttributeWithMetadataName("AutoInjectGenerator.AutoInjectAttribute"
        //     , static (node, _) => node is ClassDeclarationSyntax
        //     , static (ctx, _) => CollectInjectInfo(ctx)).Collect();

        var targets = context.SyntaxProvider.CreateSyntaxProvider(
            static (node, _) => node is ClassDeclarationSyntax cs && cs.AttributeLists.Count > 0,
            static (ctx, _) =>
            {
                var classDecl = (ClassDeclarationSyntax)ctx.Node;
                var semanticModel = ctx.SemanticModel;
                var classSymbol = (INamedTypeSymbol)semanticModel.GetDeclaredSymbol(classDecl)!;
                if (classSymbol.HasAttribute(AutoInject, true))
                {
                    return CollectInjectInfo(classSymbol, ctx.Node);
                }
                return null;
            }).Where(static i => i is not null)!.Collect();
        context.RegisterSourceOutput(context.CompilationProvider.Combine(targets), static (spc, source) =>
        {
            var (compilation, services) = source;
            var asm = compilation.Assembly;
            var codeFile = CreateModuleServiceInitClass(asm, services!);
            if (codeFile is not null)
            {
#if DEBUG
                var sss = codeFile.ToString();
#endif
                spc.AddSource(codeFile);
            }
        });
    }

    private static CodeFile? CreateModuleServiceInitClass(IAssemblySymbol asm, ImmutableArray<AutoInjectInfo> services)
    {
        var gn = NamespaceBuilder.Default.Namespace(asm.Name);
        var gclass = ClassBuilder.Default.ClassName($"AutoInjectModuleServices")
            .Attribute(AutoInjectModule)
            .AddGeneratedCodeAttribute(typeof(AutoInjectModuleGenerator));
            
        gclass.AddMembers([.. InitMethods(asm, services)]);
        return CodeFile.New($"{asm.SafeMetaName()}.AutoInject.ModuleServices.g.cs").AddUsings("using Microsoft.Extensions.DependencyInjection;")
            .AddUsings("using Microsoft.Extensions.DependencyInjection.Extensions;")
            .AddUsings("using AutoInjectGenerator.Models;")
            .AddUsings("using AutoInjectGenerator;")
            .AddMembers(gn.AddMembers(gclass));

    }
    const string SD = "AutoInjectServiceDescriptor";
    private static IEnumerable<MethodBuilder> InitMethods(IAssemblySymbol asm, ImmutableArray<AutoInjectInfo> services)
    {
        // global::System.Runtime.CompilerServices.ModuleInitializerAttribute
        var serviceName = "services";
        var configName = "config";
        var body = new List<Statement>();
        foreach (var info in services)
        {
            if (info is null || info.Services.Count == 0)
            {
                body.Add($"// No services to register for {info?.Implement}");
                continue;
            }
            body.Add($"// {info.Implement} 类型的相关注册");
            string implement = $"typeof({info.Implement})";
            var groups = info.Services.GroupBy(r => r.MemberShip)
                .Select(g => new { g.Key, Values = g.Select(gi => gi).ToArray() }).ToArray();
            foreach (var group in groups)
            {
                var groupName = group.Key;
                var item = group.Values;
                if (string.IsNullOrWhiteSpace(groupName))
                {
                    body.AddRange([.. RegisterStatements(info, item, serviceName, implement)]);
                }
                else
                {
                    var checkGroup = IfStatement.Default.If($"{configName}.ShouldInject(\"{groupName}\")")
                        .AddStatement([.. RegisterStatements(info, item, serviceName, implement)]);

                    body.Add(checkGroup);
                }

            }
        }
        //var initMethodName = $"{asm.SafeMetaName()}_Initialize";
        yield return MethodBuilder.Default.MethodName("InjectModuleServices")
            .Modifiers("public static")
            .AddParameter("IServiceCollection services", "AutoInjectConfiguration config")
            .AddGeneratedCodeAttribute(typeof(AutoInjectModuleGenerator))
            .AddBody([.. body]);


        // 模块初始化方法
        //yield return MethodBuilder.Default.MethodName($"{asm.SafeMetaName()}_ModuleInit")
        //.Modifiers("public static")
        //.Attribute("global::System.Runtime.CompilerServices.ModuleInitializerAttribute")
        //.AddGeneratedCodeAttribute(typeof(AutoInjectModuleGenerator))
        //.Lambda($"global::AutoInjectGenerator.AutoInjectManager.RegisterProjectServices(\"{asm.Name}\", {initMethodName})");

        static IEnumerable<string> RegisterStatements(AutoInjectInfo info, RegisterServiceInfo[] item, string serviceName, string implement)
        {
            // 每个分组中的Scoped是一样的，前面已经处理过
            if (item.Length == 1)
            {
                var r = item[0];
                yield return (DoCreate(serviceName, r.ServiceType, implement, r.Scoped, r.Key));
            }
            else
            {
                // 多次注入同一个服务，但是没有注入自身
                var self = item.FirstOrDefault(s => s.ServiceType == info.Implement);
                if (self is null)
                {
                    // 提供默认的自身注入
                    yield return (DoCreate(serviceName, info.Implement, implement, item[0].Scoped, null));
                }
                foreach (var r in item)
                {
                    if (r.ServiceType == info.Implement)
                    {
                        // 显式注入自身，说明前面的self不为null
                        yield return (DoCreate(serviceName, r.ServiceType, implement, r.Scoped, r.Key));
                    }
                    else
                    {
                        var factoryExpression = $"p => p.GetRequiredService<{info.Implement}>()";
                        if (r.Key is not null)
                        {
                            factoryExpression = $"(p, k) => p.GetRequiredKeyedService<{info.Implement}>(k)";
                            if (self is null || r.Key != self.Key)
                            {
                                // 注入自身
                                yield return (DoCreate(serviceName, info.Implement, implement, r.Scoped, r.Key));
                            }
                        }
                        // 注入接口
                        yield return (DoCreate(serviceName, r.ServiceType, factoryExpression, r.Scoped, r.Key, factoryReturnType: $", {implement}"));
                    }
                }
            }
        }


        static string DoCreate(string serviceName, string serviceType, string implement, string scoped, string? key, bool tryadd = false, string? factoryReturnType = "")
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
