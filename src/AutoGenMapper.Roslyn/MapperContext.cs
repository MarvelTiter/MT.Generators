using Microsoft.CodeAnalysis;
using System.Collections.Generic;
namespace AutoGenMapperGenerator;

public class MapperContext(INamedTypeSymbol source)
{
    public INamedTypeSymbol SourceType { get; } = source;
    public List<MapperTarget> Targets { get; set; } = [];
}

public class MapperTarget(INamedTypeSymbol target)
{
    public INamedTypeSymbol TargetType { get; } = target;
    public string[] ConstructorParameters { get; set; } = [];
    public List<MapInfo> Maps { get; set; } = [];
}

public class MapBetweenInfo(INamedTypeSymbol target)
{
    public DeclarePosition Position { get; set; }
    public MappingType MapType { get; set; }
    public INamedTypeSymbol Target { get; } = target;
    public string[] Sources { get; set; } = [];
    public string[] Targets { get; set; } = [];
    public string? By { get; set; }
}