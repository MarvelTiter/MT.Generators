using Microsoft.CodeAnalysis;
using System.Collections.Generic;

namespace AutoAopProxyGenerator;

internal class MethodContext(IMethodSymbol symbol, INamedTypeSymbol declared)
{
    public IMethodSymbol Symbol { get; set; } = symbol;
    public bool IsExplicit { get; set; }
    //public INamedTypeSymbol? ExplicitType { get; set; }
    public INamedTypeSymbol[] Handlers { get; set; } = [];
    public INamedTypeSymbol DeclaredType { get; set; } = declared;

}

internal record HandlerInfo(bool IsSelf, INamedTypeSymbol DeclaredType, INamedTypeSymbol Handler);