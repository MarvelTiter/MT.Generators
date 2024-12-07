using System;
using System.Linq;
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
        context.RegisterSourceOutput(context.CompilationProvider, static (context, compilation) =>
        {
            try
            {
                if (!compilation.Assembly.HasAttribute(ApiInvokerAssemblyAttributeFullName))
                {
                    return;
                }

                var all = compilation.GetAllSymbols(ApiInvokerAttributeFullName);
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