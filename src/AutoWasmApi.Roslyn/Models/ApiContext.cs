using Generators.Shared;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace AutoWasmApiGenerator.Models;

internal class ApiContext(INamedTypeSymbol namedTypeSymbol)
{
    public string? RouteUrl { get; set; }
    public string TypeName => InterfaceType.ToDisplayString();
    public string FileName => InterfaceType.FormatClassName();
    public string NameSpace => InterfaceType.ContainingNamespace.ToDisplayString();
    public INamedTypeSymbol InterfaceType { get; set; } = namedTypeSymbol;
    public AuthorizeInfo AuthorizeInfo { get; set; } = new();

    public List<ApiMethodInfo> Methods { get; set; } = [];

}

internal class AuthorizeInfo
{
    public bool AllowAnonymous { get; set; }
    public bool RequiredAuthorize { get; set; }
    public string? AuthorizeScheme { get; set; }
}

internal class ApiMethodInfo(IMethodSymbol symbol)
{
    public IMethodSymbol Symbol { get; set; } = symbol;
    [NotNull] public string? RouteUrl { get; set; }
    public AuthorizeInfo MethodAuthorize { get; set; } = new();
    [NotNull] public string? HttpMethod { get; set; }

}