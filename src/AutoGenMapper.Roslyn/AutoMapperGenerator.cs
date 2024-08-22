using System;
using System.Collections.Generic;
using System.Linq;
using Generators.Shared.Builder;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Generators.Shared;
using System.Diagnostics;

namespace AutoGenMapperGenerator
{
    [Generator(LanguageNames.CSharp)]
    public class AutoMapperGenerator : IIncrementalGenerator
    {
        const string GenMapperAttributeFullName = "AutoGenMapperGenerator.GenMapperAttribute";
        const string GenMapFromAttributeFullName = "AutoGenMapperGenerator.MapFromAttribute";
        const string GenMapToAttributeFullName = "AutoGenMapperGenerator.MapToAttribute";
        const string GenMapableInterface = "AutoGenMapperGenerator.IAutoMap";
        const string GenMapIgnoreAttribute = "AutoGenMapperGenerator.MapIgnore";

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var map = context.SyntaxProvider.ForAttributeWithMetadataName(GenMapperAttributeFullName
                , static (node, _) => node is ClassDeclarationSyntax
                , static (source, _) => source);
            context.RegisterSourceOutput(map, static (context, source) =>
            {
                var file = CreateCodeFile(context, source);
                context.AddSource(file);
            });
        }

        private static CodeFile? CreateCodeFile(SourceProductionContext context, GeneratorAttributeSyntaxContext source)
        {
            var origin = (INamedTypeSymbol)source.TargetSymbol;
            if (!origin.HasInterface(GenMapableInterface))
            {
                context.ReportDiagnostic(DiagnosticDefinitions.AGM00001(source.TargetNode.GetLocation()));
                return null;
            }

            var ctxs = source.TargetSymbol.GetAttributes(GenMapperAttributeFullName).Select(s => CollectTypeInfos(origin, s)).ToArray();
            var cb = ClassBuilder.Default.Modifiers("partial").ClassName(origin.Name)
                .AddGeneratedCodeAttribute(typeof(AutoMapperGenerator));
            List<MethodBuilder> methods = [];
            foreach (var ctx in ctxs)
            {
                var m = BuildAutoMapClass.GenerateMethod(ctx);
                methods.Add(m);
            }

            var im = BuildAutoMapClass.GenerateInterfaceMethod(ctxs);
            methods.Add(im);
            cb.AddMembers([.. methods]);

            var cla = cb.ToString();
            return CodeFile.New($"{origin.FormatFileName()}.AutoMap.g.cs")
                .AddMembers(NamespaceBuilder.Default.Namespace(origin.ContainingNamespace.ToDisplayString())
                    .AddMembers(cb));
        }

        private static GenMapperContext CollectTypeInfos(INamedTypeSymbol source, AttributeData a)
        {
            var context = new GenMapperContext() { SourceType = source };
            context.SourceProperties = source.GetMembers().Where(i => i.Kind == SymbolKind.Property)
                    .Cast<IPropertySymbol>().ToArray();
            if (!(a.GetNamedValue("TargetType", out var t) && t is INamedTypeSymbol target))
            {
                target = (a.ConstructorArguments.FirstOrDefault().Value as INamedTypeSymbol) ?? source;
            }

            if (a.ConstructorArguments.Length > 1)
            {
                context.ConstructorParameters =
                    a.ConstructorArguments[1].Values.Where(c => c.Value != null).Select(c => c.Value!.ToString()).ToArray();
            }

            context.TargetType = target;

            context.TargetProperties = target.GetMembers().Where(i => i.Kind == SymbolKind.Property)
                .Cast<IPropertySymbol>().ToArray();
            //Debugger.Launch();
            foreach (var (froms, tos) in context.TargetProperties.Select(GetMapInfo))
            {
                context.Froms.AddRange(froms);
                context.Tos.AddRange(tos);
            }
            foreach (var (froms, tos) in context.SourceProperties.Select(GetMapInfo))
            {
                context.Froms.AddRange(froms);
                context.Tos.AddRange(tos);
            }

            return context;
        }

        private static (MapInfo[] Froms, MapInfo[] Tos) GetMapInfo(ISymbol symbol)
        {
            if (symbol is not IPropertySymbol prop)
            {
                return ([], []);
            }

            var from = prop.GetAttributes(GenMapFromAttributeFullName).Select(a =>
            {
                var mapInfo = new MapInfo();
                if (a.GetNamedValue("Source", out var t) && t is INamedTypeSymbol target)
                {
                    mapInfo.Target = target;
                }

                if (a.GetNamedValue("Name", out var n))
                {
                    mapInfo.From = n!.ToString();
                }

                if (a.GetNamedValue("By", out var b))
                {
                    mapInfo.By = prop.ContainingType.GetMembers().FirstOrDefault(s => s.Kind == SymbolKind.Method && s.Name == b?.ToString()) as IMethodSymbol;
                }
                mapInfo.To = prop.Name;

                return mapInfo;
            }).ToArray();

            var to = prop.GetAttributes(GenMapToAttributeFullName).Select(a =>
            {
                var mapInfo = new MapInfo();
                if (a.GetNamedValue("Target", out var t) && t is INamedTypeSymbol target)
                {
                    mapInfo.Target = target;
                }

                if (a.GetNamedValue("Name", out var n))
                {
                    mapInfo.To = n!.ToString();
                }

                if (a.GetNamedValue("By", out var b))
                {
                    mapInfo.By = prop.ContainingType.GetMembers().FirstOrDefault(s => s.Kind == SymbolKind.Method && s.Name == b?.ToString()) as IMethodSymbol;
                }
                mapInfo.From = prop.Name;

                return mapInfo;
            }).ToArray();

            return (from, to);
        }
    }
}