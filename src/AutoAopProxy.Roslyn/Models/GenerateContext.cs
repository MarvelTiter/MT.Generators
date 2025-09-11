using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Text;

namespace AutoAopProxyGenerator.Models;

internal class GenerateContext(INamedTypeSymbol targetSymbol)
{
    public INamedTypeSymbol TargetSymbol { get; } = targetSymbol;
    public INamedTypeSymbol[] ProxyInterfaces { get; set; } = [];
    public List<HandlerInfo> AllHandlers { get; set; } = [];
    public List<AspectMethodContext> AllMethods { get; set; } = [];
    public Diagnostic? Diagnostic { get; set; }
}

//internal class HandlerInfo
//{
//    public string Type { get; set; }
//    public string Name { get; set; }
//}

internal class AspectMethodContext(IMethodSymbol symbol, INamedTypeSymbol declared)
{
    public IMethodSymbol Symbol { get; } = symbol;
    public INamedTypeSymbol DeclaredType { get; } = declared;
    public bool IsIgnoreAll { get; set; }
    public bool IsExplicit { get; set; }
    public INamedTypeSymbol[] MethodHandlers { get; set; } = [];
    public INamedTypeSymbol[] IgnoreHandlers { get; set; } = [];
}


