using Generators.Shared;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AutoGenMapperGenerator;


[Generator(LanguageNames.CSharp)]
public class AutoMapperFromGenerator : IIncrementalGenerator
{
    const string AutoMapperFromAttribute = "AutoGenMapperGenerator.GenMapperFromAttribute";
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var targets = context.SyntaxProvider.ForAttributeWithMetadataName(
            AutoMapperFromAttribute
            , static (_, _) => true
            , static (context, _) => context).Collect();

        context.RegisterSourceOutput(targets, static (context, source) =>
        {
            Dictionary<INamedTypeSymbol, List<GenMapperContext>> dic = new(EqualityComparer<INamedTypeSymbol>.Default);
            foreach (var item in source)
            {
                var dtoCLass = (INamedTypeSymbol)item.TargetSymbol;
                dtoCLass.GetAttribute(AutoMapperFromAttribute, out var config);
                var (sourceType, exclude, cfg) = ExtractAttriteData(config!);
                if (sourceType is null)
                {
                    context.ReportDiagnostic(DiagnosticDefinitions.AGM00014(dtoCLass.TryGetLocation()));
                    return;
                }
                if (!dic.TryGetValue(sourceType, out var list))
                {
                    list = [];
                    dic.Add(sourceType, list);
                }

            }

        });

    }

    private static (INamedTypeSymbol? sourceType, string[] exclude, INamedTypeSymbol? cfg) ExtractAttriteData(AttributeData config)
    {
        if (config.ConstructorArguments.Length > 0)
        {
            config.GetConstructorValue(0, out var source);
            config.GetConstructorValues(1, out var exclude);
            var cfg = config.GetNamedValue("Configuration");
            return (source as INamedTypeSymbol, [.. exclude.Where(o => o is not null).Select(o => o!.ToString())], cfg as INamedTypeSymbol);
        }
        else
        {
            var source = config.GetNamedValue("SourceType");
            var exclude = config.GetNamedValues("Exclude");
            var cfg = config.GetNamedValue("Configuration");
            return (source as INamedTypeSymbol, [.. exclude.Where(o => o is not null).Select(o => o!.ToString())], cfg as INamedTypeSymbol);
        }
    }
}
