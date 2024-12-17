using System;
using System.Linq;
using AutoWasmApiGenerator.Options;
using Generators.Shared;
using Microsoft.CodeAnalysis;
using static AutoWasmApiGenerator.GeneratorHelpers;

namespace AutoWasmApiGenerator;

[Generator(LanguageNames.CSharp)]
public class ControllerGenerator : IIncrementalGenerator
{
    private static readonly IControllerGenerator Generator = new ControllerGeneratorImpl();
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var globalOptions = context.AnalyzerConfigOptionsProvider.Select(GlobalOptions.Select);

        context.RegisterSourceOutput(context.CompilationProvider.Combine(globalOptions), static (context, compilationData) =>
        {
            try
            {
                var compilation = compilationData.Left;
                var globalOptions = compilationData.Right;
                var generate = compilation.Assembly.HasAttribute(WebControllerAssemblyAttributeFullName)
                               || globalOptions.GenerateWebController;

                if (!generate)
                {
                    return;
                }
                var all = compilation.GetAllSymbols(WebControllerAttributeFullName);
                foreach (var item in all)
                {
                    var (generatedFileName, sourceCode, errorAndWarnings) = Generator.Generate(item);
                    if (errorAndWarnings.Any())
                    {
                        foreach (var ew in errorAndWarnings)
                        {
                            context.ReportDiagnostic(ew);
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
                context.ReportDiagnostic(DiagnosticDefinitions.WAG00012(Location.None, ex.Message));
            }
        });
    }
}