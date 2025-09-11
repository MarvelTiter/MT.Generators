using Generators.Shared;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Text;

namespace AutoPageStateContainerGenerator.Models;

internal class StateContext(INamedTypeSymbol targetSymbol)
{
    public INamedTypeSymbol TargetSymbol { get; set; } = targetSymbol;
    public object? Lifetime { get; set; }
    public string? CustomName { get; set; }
    public INamedTypeSymbol? Implements { get; set; }
    public List<FieldMember> Fields { get; set; } = [];
    public List<PropertyMember> Properties { get; set; } = [];

    public string TypeName { get; } = $"{targetSymbol.FormatClassName(true)}StateContainer";

    public Diagnostic? Diagnostic { get; set; }
}

internal class FieldMember(string name
    , INamedTypeSymbol containingType
    , string type
    , string? init = null)
{
    public string Name { get; set; } = name;
    public INamedTypeSymbol ContainingType { get; } = containingType;
    public string? Init { get; set; } = init;
    public string Type { get; set; } = type;
}

internal class  PropertyMember(string name
    , INamedTypeSymbol containingType
    , string type
    , bool isVirtual = false
    , string? init = null) :FieldMember(name,containingType,type,init)
{
    public bool IsVirtual { get; set; } = isVirtual;
}