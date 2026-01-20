using Generators.Shared;
using Generators.Shared.Builder;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using static AutoInjectGenerator.AutoInjectContextGeneratorHelpers;
namespace AutoInjectGenerator;

[Generator(LanguageNames.CSharp)]
public class AutoInjectEntryGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var value = context.SyntaxProvider.ForAttributeWithMetadataName(AutoInjectContext
            , static (node, _) => node is ClassDeclarationSyntax
            , static (ctx, _) => CollectContextInfo(ctx, _));
        context.RegisterSourceOutput(value, static (spc, source) =>
        {
            var target = source.TargetSymbol;
            var asm = target.ContainingAssembly;
            var modules = new List<INamedTypeSymbol>();
            source.ContainSelf = asm.GlobalNamespace?.GetAllMembers<INamedTypeSymbol>(i => i is INamedTypeSymbol m && m.HasAttribute(AutoInject, true)).Any();

            foreach (var item in asm.Modules)
            {
                foreach (var referencedAssembly in item.ReferencedAssemblySymbols)
                {
                    if (referencedAssembly.Name.StartsWith("System.") || referencedAssembly.Name.StartsWith("Microsoft."))
                        continue;

                    if (referencedAssembly.GlobalNamespace is null) continue;
                    modules.AddRange(referencedAssembly.GlobalNamespace.GetAllMembers<INamedTypeSymbol>(i => i is INamedTypeSymbol m && m.HasAttribute(AutoInjectModule)));
                }
            }
            var file = CreateCodeFile(source, modules);
#if DEBUG
            var sss = file?.ToString();
#endif
            spc.AddSource(file);
        });
    }

    private static CodeFile? CreateCodeFile(AutoInjectContextInfo context, List<INamedTypeSymbol> modules)
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

        var includeField = FieldBuilder.Default
            .Modifiers("private static readonly")
            .MemberType("global::System.Collections.Generic.List<string>")
            .FieldName("_includes")
            .InitializeWith($"[{string.Join(", ", context.Includes.Select(s => $"\"{s}\""))}] ");
        var excludeField = FieldBuilder.Default
            .Modifiers("private static readonly")
            .MemberType("global::System.Collections.Generic.List<string>")
            .FieldName("_excludes")
            .InitializeWith($"[{string.Join(", ", context.Excludes.Select(s => $"\"{s}\""))}] ");

        var cm = MethodBuilder.Default.Partial(methodSymbol);
        {
            // 创建配置
            List<string> methodBody = [
                "var config = new global::AutoInjectGenerator.AutoInjectConfiguration(_excludes, _includes)",
                //$"global::AutoInjectGenerator.AutoInjectManager.ApplyProjectServices({serviceName}, config)"
                ];
            if (context.ContainSelf == true)
            {
                methodBody.Add($"global::{context.TargetSymbol.ContainingAssembly.Name}.AutoInjectModuleServices.InjectModuleServices({serviceName}, config)");
            }
            foreach (var item in modules)
            {
                methodBody.Add($"global::{item.ContainingAssembly.Name}.AutoInjectModuleServices.InjectModuleServices({serviceName}, config)");
            }
            cm.AddBody([.. methodBody]);
        }
        gclass.AddMembers(includeField);
        gclass.AddMembers(excludeField);
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
}
