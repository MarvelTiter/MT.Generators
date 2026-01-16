using Generators.Shared;
using Generators.Shared.Builder;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
namespace AutoGenMapperGenerator;

internal partial class Helper
{
    internal const string GenMapperAttributeFullName = "AutoGenMapperGenerator.GenMapperAttribute";
    internal const string GenMapFromAttributeFullName = "AutoGenMapperGenerator.MapFromAttribute";
    internal const string GenMapToAttributeFullName = "AutoGenMapperGenerator.MapToAttribute";
    internal const string GenMapableInterface = "AutoGenMapperGenerator.IAutoMap";
    internal const string GenMapIgnoreAttribute = "AutoGenMapperGenerator.MapIgnoreAttribute";
    internal const string GenMapBetweenAttributeFullName = "AutoGenMapperGenerator.MapBetweenAttribute";
    internal const string GenMapConstructorAttributeFullName = "AutoGenMapperGenerator.MapConstructorAttribute";

    public static Diagnostic? CollectMapperContext(MapperContext context
        , MapBetweenInfo[] mapBetweens
        , Location? location)
    {
        //var hasDefaultCtor = source.Constructors.Any(c => c.Parameters.Length == 0);
        //if (!hasDefaultCtor)
        //{
        //    return (default, DiagnosticDefinitions.AGM00011(location));
        //}
        Diagnostic? error = null;
        var source = context.SourceType;
        var containType = context.ContainingType;
        var sourceProperties = source.GetAllMembers(_ => true).Where(i => i.Kind == SymbolKind.Property)
                .Cast<IPropertySymbol>().ToArray();

        foreach (var mapTarget in context.Targets)
        {
            var target = mapTarget.TargetType;
            var targetProperties = target.GetAllMembers(_ => true).Where(i => i.Kind == SymbolKind.Property)
                .Cast<IPropertySymbol>().ToArray();
            var specificBetweens = mapBetweens.Where(t => EqualityComparer<INamedTypeSymbol>.Default.Equals(target, t.Target)).ToList();
            foreach (var map in specificBetweens)
            {
                var mapInfo = new MapInfo()
                {
                    Position = map.Position,
                    MappingType = map.MapType,
                    SourceName = map.Sources,
                    TargetName = map.Targets
                };
                mapInfo.SourceProp = [.. sourceProperties.Where(p => mapInfo.SourceName.Contains(p.Name))];
                mapInfo.TargetProp = [.. targetProperties.Where(p => mapInfo.TargetName.Contains(p.Name))];
                if (map.By is not null)
                {
                    error = HandleByMethod(containType, map.By, mapInfo, location);
                    if (error is not null)
                    {
                        return error;
                    }
                }
                mapTarget.Maps.Add(mapInfo);
            }

            error = HandleAutoMapProperties(mapTarget.Maps
               , source
               , target
               , sourceProperties
               , targetProperties
               , location);
            if (error is not null)
            {
                return error;
            }
        }
        return error;
    }

    private static Diagnostic? HandleAutoMapProperties(List<MapInfo> mapInfos
        , INamedTypeSymbol source
        , INamedTypeSymbol target
        , IPropertySymbol[] sourceProperties
        , IPropertySymbol[] targetProperties
        , Location? location)
    {
        foreach (var sp in sourceProperties)
        {
            if (sp.IsReadOnly || sp.DeclaredAccessibility != Accessibility.Public)
            {
                continue;
            }

            var mi = new MapInfo()
            {
                SourceName = [sp.Name],
                SourceProp = [sp],
                MappingType = MappingType.SingleToSingle
            };
            // 属性上可能有多个Attribute，只找出跟当前Target一样的Target的Attribute
            var AttrData = GetSpecificAttribute(sp, target, GenMapBetweenAttributeFullName);
            if (AttrData is not null)
            {
                if (AttrData.ConstructorArguments.Length > 2)
                {
                    return DiagnosticDefinitions.AGM00005(location);
                }

                AttrData.GetConstructorValue(1, out var tarName);
                // 已经在Class上定义了MapBetween，就跳过
                if (mapInfos.Any(m => m.Position == DeclarePosition.Class
                                      && m.SourceName.Contains(sp.Name)
                                      && m.TargetName.Contains(tarName!.ToString())))
                {
                    continue;
                }

                mi.TargetName = [tarName!.ToString()];
                mi.TargetProp = [.. targetProperties.Where(p => mi.TargetName.Contains(p.Name))];
                if (AttrData.GetNamedValue("By", out var by))
                {
                    var methodName = by!.ToString();
                    var error = HandleByMethod(source, methodName, mi, location);
                    if (error is not null)
                    {
                        return error;
                    }
                }
            }
            else
            {
                if (sp.HasAttribute(GenMapIgnoreAttribute))
                {
                    continue;
                }
                var tp = targetProperties.FirstOrDefault(tp => tp.Name == sp.Name);
                if (tp is null)
                {
                    continue;
                }

                mi.TargetName = [tp.Name];
                mi.TargetProp = [tp];
            }

            mapInfos.Add(mi);
        }
        return null;
    }

    private static AttributeData? GetSpecificAttribute(ISymbol property
        , INamedTypeSymbol matchSymbol
        , string fullName)
    {
        var attrs = property.GetAttributes(fullName);
        foreach (var item in attrs)
        {
            item.GetConstructorValue(0, out var t);
            var symbol = (INamedTypeSymbol)t!;
            if (EqualityComparer<INamedTypeSymbol>.Default.Equals(symbol, matchSymbol))
            {
                return item;
            }
        }

        return null;
    }

    private static Diagnostic? HandleByMethod(ISymbol declared
        , string methodName
        , MapInfo mi
        , Location? location)
    {
        var declaredType = declared switch
        {
            INamedTypeSymbol nts => nts,
            IMethodSymbol ms => ms.ContainingType,
            _ => throw new InvalidOperationException("Unsupported symbol type for 'declared'.")
        };
        var methods = declaredType.GetAllMembers(_ => true).Where(i => i.Kind == SymbolKind.Method && i.IsStatic && i.Name == methodName).Cast<IMethodSymbol>().ToArray();
        if (methods.Length == 0)
        {
            return DiagnosticDefinitions.AGM00003(location);
        }

        var spts = mi.SourceProp.Select(p => p.Type).ToArray();
        var tpts = mi.TargetProp.Select(p => p.Type).ToArray();

        #region 正向映射

        var forward = methods.FirstOrDefault(m => CheckParameters(m, spts));
        if (!CheckMappingMethodReturnType(forward
                , location
                , spts
                , tpts
                , out var error))
        {
            return error;
        }

        mi.ForwardBy = forward;

        #endregion

        #region 反向映射

        var reverse = methods.FirstOrDefault(m => CheckParameters(m, tpts));
        if (CheckMappingMethodReturnType(reverse
                , location
                , tpts
                , spts
                , out error))
        {
            mi.CanReverse = true;
            mi.ReverseBy = reverse;
        }
        else if (reverse != null)
        {
            return error;
        }

        #endregion

        return null;
    }

    private static bool CheckParameters(IMethodSymbol method, ITypeSymbol[] paramTypes)
    {
        // 检查参数类型
        if (method.Parameters.Length != paramTypes.Length) return false;
        for (int i = 0; i < method.Parameters.Length; i++)
        {
            var mpt = method.Parameters[i].Type;
            if (!EqualityComparer<ITypeSymbol>.Default.Equals(mpt, paramTypes[i]))
            {
                return false;
            }
        }
        return true;
    }

    private static bool CheckMappingMethodReturnType(IMethodSymbol? method
        , Location? location
        , ITypeSymbol[] paramTypes
        , ITypeSymbol[] returnTypes
        , out Diagnostic? error)
    {
        if (method is null)
        {
            // 自定义处理方法的参数个数不匹配
            error = DiagnosticDefinitions.AGM00007(location);
            return false;
        }
        location = method.Locations.FirstOrDefault();
        // 检查返回值
        if (returnTypes.Length > 1)
        {
            // 一对多的情况，返回值需要是object[]类型
            if (!method.ReturnType.IsTupleType)
            {
                error = DiagnosticDefinitions.AGM00009(location);
                return false;
            }
            if (method.ReturnType is not INamedTypeSymbol tuple)
            {
                error = DiagnosticDefinitions.AGM00009(location);
                return false;
            }
            var tupleElement = tuple.TupleElements;
            if (tupleElement.Length != returnTypes.Length)
            {
                error = DiagnosticDefinitions.AGM00010(location);
                return false;
            }
            for (int i = 0; i < tupleElement.Length; i++)
            {
                var te = tupleElement[i].Type;
                if (!EqualityComparer<ITypeSymbol>.Default.Equals(te, returnTypes[i]))
                {
                    error = DiagnosticDefinitions.AGM00008(location);
                    return false;
                }
            }
        }
        else
        {
            var returnType = returnTypes[0];
            if (!EqualityComparer<ITypeSymbol>.Default.Equals(method.ReturnType, returnType))
            {
                error = DiagnosticDefinitions.AGM00009(location);
                return false;
            }
        }

        error = null;
        return true;
    }
}
