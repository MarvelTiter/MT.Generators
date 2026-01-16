using Generators.Shared;
using Generators.Shared.Builder;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;
using static AutoGenMapperGenerator.Helper;
namespace AutoGenMapperGenerator;

[Generator(LanguageNames.CSharp)]
public class AutoMapperExtensionGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var map = context.SyntaxProvider.ForAttributeWithMetadataName(GenMapperAttributeFullName
            , static (node, _) => node is MethodDeclarationSyntax
            {
                ParameterList.Parameters:
                {
                    Count: var psCount
                } ps
            } && psCount > 0 && ps[0].Type is not null
            , static (source, _) => CollectMapperContext(source));

        context.RegisterSourceOutput(map, static (context, source) =>
        {
            if (source.Item2 is not null)
            {
                context.ReportDiagnostic(source.Item2);
                return;
            }
            if (source.Item1 is null) return;
            var file = CreateCodeFile(source.Item1);
#if DEBUG
            var ss = file?.ToString();
#endif
            context.AddSource(file);
        });
    }
    static (MapperContext?, Diagnostic?) CollectMapperContext(GeneratorAttributeSyntaxContext context)
    {
        var location = context.TargetNode.GetLocation();
        var mapBetweens = CollectSpecificBetweenInfo(context.TargetSymbol).ToArray();
        var mapperTargets = CollectMapTargets(context.TargetSymbol);
        var mapContext = new MapperContext(context.TargetSymbol)
        {
            Targets = mapperTargets
        };
        var error = Helper.CollectMapperContext(mapContext, mapBetweens, location);
        return (mapContext, error);
    }
    static CodeFile? CreateCodeFile(MapperContext context)
    {
        INamedTypeSymbol classSymbol = context.ContainingType!;
        var cb = ClassBuilder.Default.Modifiers("static partial").ClassName(classSymbol.Name)
            .AddGeneratedCodeAttribute(typeof(AutoMapperGenerator));
        List<MethodBuilder> methods = [];
        foreach (var ctx in context.Targets)
        {
            var m = BuildAutoMapClass.GenerateExtensionMethod(context, ctx);
            methods.Add(m);
        }

        cb.AddMembers([.. methods]);
        var ns = NamespaceBuilder.Default.Namespace(classSymbol.ContainingNamespace.ToDisplayString());
        return CodeFile.New($"{classSymbol.FormatFileName()}.AutoMap.Ex.g.cs")
            //.AddUsings("using System.Linq;")
            //.AddUsings("using AutoGenMapperGenerator;")
            .AddUsings(classSymbol.GetTargetUsings())
            .AddMembers(ns.AddMembers(cb));
    }
}
