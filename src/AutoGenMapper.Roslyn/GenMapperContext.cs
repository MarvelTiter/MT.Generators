using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
namespace AutoGenMapperGenerator;

public enum MappingType
{
    SingleToSingle,
    SingleToMulti,
    MultiToSingle,
}

public enum DeclarePosition
{
    Property,
    Class,
    Method,
}

public class MapInfo
{
    public List<string> SourceName { get; set; } = [];
    public List<string> TargetName { get; set; } = [];
    public List<IPropertySymbol> SourceProp { get; set; } = [];
    public List<IPropertySymbol> TargetProp { get; set; } = [];
    public IMethodSymbol? ForwardBy { get; set; }
    public IMethodSymbol? ReverseBy { get; set; }
    public INamedTypeSymbol? MapType { get; set; }
    public MappingType MappingType { get; set; }
    public DeclarePosition Position { get; set; }
    public bool CanReverse { get; set; }
}

[Obsolete]
public class GenMapperContext
{
    public INamedTypeSymbol SourceType { get; set; } = default!;
    public INamedTypeSymbol TargetType { get; set; } = default!;
    //public IPropertySymbol[] SourceProperties { get; set; } = [];
    //public IPropertySymbol[] TargetProperties { get; set; } = [];
    public string[] ConstructorParameters { get; set; } = [];
    //public List<MapInfo> Froms { get; set; } = [];
    //public List<MapInfo> Tos { get; set; } = [];
    public List<MapInfo> Maps { get; set; } = [];
}
