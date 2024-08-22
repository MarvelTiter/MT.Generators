using System.Collections.Generic;
using Microsoft.CodeAnalysis;
namespace AutoGenMapperGenerator;

public class MapInfo
{
    public INamedTypeSymbol Target { get; set; } = default!;
    public string? From { get; set; }
    public string? To { get; set; }
    public IMethodSymbol? By { get; set; }
}
public class GenMapperContext
{
    public INamedTypeSymbol SourceType { get; set; } = default!;
    public INamedTypeSymbol TargetType { get; set; } = default!;
    public IPropertySymbol[] SourceProperties { get; set; } = [];
    public IPropertySymbol[] TargetProperties { get; set; } = [];
    public string[] ConstructorParameters { get; set; } = [];
    public List<MapInfo> Froms { get; set; } = [];
    public List<MapInfo> Tos { get; set; } = [];
}