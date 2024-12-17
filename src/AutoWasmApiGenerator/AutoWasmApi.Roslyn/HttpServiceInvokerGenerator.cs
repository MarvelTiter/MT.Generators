using System;
using System.Diagnostics;
using System.Linq;
using AutoWasmApiGenerator.Options;
using Generators.Shared;
using Microsoft.CodeAnalysis;
using static AutoWasmApiGenerator.GeneratorHelpers;

namespace AutoWasmApiGenerator;

[Generator(LanguageNames.CSharp)]
public class HttpServiceInvokerGenerator : IIncrementalGenerator
{
    private static readonly HttpServiceInvokerGeneratorImpl Generator = new();

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var globalOptions = context.AnalyzerConfigOptionsProvider.Select(GlobalOptions.Select);
        context.RegisterSourceOutput(context.CompilationProvider.Combine(globalOptions), static (context, compilationData) =>
        {
            try
            {
                var compilation = compilationData.Left;
                var globalOptions = compilationData.Right;
                var generate = compilation.Assembly.HasAttribute(ApiInvokerAssemblyAttributeFullName)
                               || globalOptions.GenerateInvoker;
                if (!generate)
                {
                    return;
                }

                var all = compilation.GetAllSymbols(ApiInvokerGenerateAttributeFullName);

                foreach (var item in all)
                {
                    if (!item.HasAttribute(WebControllerAttributeFullName))
                    {
                        foreach (var location in item.Locations)
                        {
                            context.ReportDiagnostic(DiagnosticDefinitions.WAG00011(location));
                        }
                        continue;
                    }

                    var (generatedFileName, sourceCode, errorAndWarnings) = Generator.Generate(item);
                    if (errorAndWarnings.Any())
                    {
                        foreach (var item1 in errorAndWarnings)
                        {
                            context.ReportDiagnostic(item1);
                        }
                    }
                    else
                    {
                        context.AddSource(generatedFileName, sourceCode);
                    }
                }
            }
            catch (Exception ex)
            {
                context.ReportDiagnostic(DiagnosticDefinitions.WAG00010(Location.None, ex.Message));
            }
        });
    }
}