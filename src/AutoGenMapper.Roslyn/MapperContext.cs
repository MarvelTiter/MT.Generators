using Microsoft.CodeAnalysis;
using System.Collections.Generic;
namespace AutoGenMapperGenerator;

public class MapperContext
{
    public MapperContext(ISymbol targetSymbol)
    {
        TargetSymbol = targetSymbol;
        if (targetSymbol is INamedTypeSymbol namedTypeSymbol)
        {
            ContainingType = namedTypeSymbol;
            SourceType = namedTypeSymbol;
        }
        else if (targetSymbol is IMethodSymbol methodSymbol)
        {
            ContainingType = methodSymbol.ContainingType;
            SourceType = (INamedTypeSymbol)methodSymbol.Parameters[0].Type;
        }
        else
            throw new System.InvalidOperationException();
    }
    public ISymbol TargetSymbol { get; }
    public INamedTypeSymbol ContainingType { get; }
    public INamedTypeSymbol SourceType { get; }
    public List<MapperTarget> Targets { get; set; } = [];
}

public readonly struct MapperGenerateContext(INamedTypeSymbol source, MapperTarget target, string objectName)
{
    public INamedTypeSymbol Source { get; } = source;
    public MapperTarget Target { get; } = target;
    public string ObjectName { get; } = objectName;
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