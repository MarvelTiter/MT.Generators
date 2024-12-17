using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace AutoWasmApiGenerator;

public interface IHttpServiceInvokerGenerator
{
    (string generatedFileName, string sourceCode, List<Diagnostic> errorAndWarnings) Generate(
        INamedTypeSymbol interfaceSymbol);
}