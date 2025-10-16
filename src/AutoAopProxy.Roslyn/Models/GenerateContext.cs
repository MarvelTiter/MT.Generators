using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Text;

namespace AutoAopProxyGenerator.Models;

internal class GenerateContext(INamedTypeSymbol targetSymbol, AttributeData attributeData)
{
    public INamedTypeSymbol TargetSymbol { get; } = targetSymbol;
    public AttributeData AttributeData { get; set; } = attributeData;
    public INamedTypeSymbol[] ProxyInterfaces { get; set; } = [];
    public List<HandlerInfo> AllHandlers { get; set; } = [];
    public List<AspectMemberContext> AllMethods { get; set; } = [];
    public Diagnostic? Diagnostic { get; set; }
}

//internal class HandlerInfo
//{
//    public string Type { get; set; }
//    public string Name { get; set; }
//}
internal enum HandlerType
{
    Method,
    Property,
}
internal class AspectMemberContext(ISymbol symbol, INamedTypeSymbol declared)
{
    public ISymbol Symbol { get; } = symbol;
    public IMethodSymbol MethodSymbol
    {
        get
        {
            if (Symbol is IMethodSymbol ms)
                return ms;
            throw new InvalidOperationException("Symbol is not a method.");
        }
    }
    public IPropertySymbol PropertySymbol
    {
        get
        {
            if (Symbol is IPropertySymbol ps)
                return ps;
            throw new InvalidOperationException("Symbol is not a property.");
        }
    }
    public HandlerType Type { get; set; }
    public INamedTypeSymbol DeclaredType { get; } = declared;
    public bool IsOverride { get; set; }
    public bool IsIgnoreAll { get; set; }
    public bool IsExplicit { get; set; }
    public INamedTypeSymbol[] MethodHandlers { get; set; } = [];
    public INamedTypeSymbol[] IgnoreHandlers { get; set; } = [];
}


