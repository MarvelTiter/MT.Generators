using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Generators.Shared;
using Generators.Shared.Builder;
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

    public static CodeFile? CreateCodeFile(MapperContext context)
    {
        INamedTypeSymbol source = context.SourceType;
        var cb = ClassBuilder.Default.Modifiers("partial").ClassName(source.Name)
            .Interface(GenMapableInterface)
            .AddGeneratedCodeAttribute(typeof(AutoMapperGenerator));
        List<MethodBuilder> methods = [];
        foreach (var ctx in context.Targets)
        {
            var m = BuildAutoMapClass.GenerateMapToMethod(source, ctx);
            methods.Add(m);
            var f = BuildAutoMapClass.GenerateMapFromMethod(source, ctx);
            methods.Add(f);
        }

        var im = BuildAutoMapClass.GenerateInterfaceMethod(context.Targets);
        methods.AddRange(im);
        cb.AddMembers([.. methods]);
        var ns = NamespaceBuilder.Default.Namespace(source.ContainingNamespace.ToDisplayString());
        return CodeFile.New($"{source.FormatFileName()}.AutoMap.g.cs")
            //.AddUsings("using System.Linq;")
            //.AddUsings("using AutoGenMapperGenerator;")
            .AddUsings(source.GetTargetUsings())
            .AddMembers(ns.AddMembers(cb));
    }

    public static (MapperContext?, Diagnostic?) CollectMapperContextFromClass(GeneratorAttributeSyntaxContext context)
    {
        var source = (INamedTypeSymbol)context.TargetSymbol;
        var location = context.TargetNode.GetLocation();
        var mapBetweens = CollectSpecificBetweenInfo(source).ToArray();
        var mapperTargets = CollectMapTargets(source);
        return CollectMapperContext(mapperTargets, mapBetweens, source, location);
    }

    private static (MapperContext?, Diagnostic?) CollectMapperContext(List<MapperTarget> mapperTargets
        , MapBetweenInfo[] mapBetweens
        , INamedTypeSymbol source
        , Location? location)
    {
        //var hasDefaultCtor = source.Constructors.Any(c => c.Parameters.Length == 0);
        //if (!hasDefaultCtor)
        //{
        //    return (default, DiagnosticDefinitions.AGM00011(location));
        //}
        var mapCtx = new MapperContext(source);

        var sourceProperties = source.GetAllMembers(_ => true).Where(i => i.Kind == SymbolKind.Property)
                .Cast<IPropertySymbol>().ToArray();

        foreach (var mapTarget in mapperTargets)
        {
            var target = mapTarget.TargetType;
            foreach (var map in mapBetweens.Where(t => EqualityComparer<INamedTypeSymbol>.Default.Equals(target, t.Target)))
            {

            }

            //var targetProperties = a.TargetType.GetAllMembers(_ => true).Where(i => i.Kind == SymbolKind.Property).Cast<IPropertySymbol>().ToArray();

            var mapInfos = mapTarget.Maps;


            mapCtx.Targets.Add(mapTarget);
        }



        return (mapCtx, null);
    }

    private static Diagnostic? HandleMapBetweens(List<MapInfo> maps
        , IPropertySymbol[] sourceProperties
        , IPropertySymbol[] targetProperties
        , INamedTypeSymbol declared
        , Location? location)
    {
        var mi = new MapInfo() { Position = DeclarePosition.Class };
        var type = CheckMapType(item, 1);
        item.GetConstructorValue(0, out var tar);
        switch (type)
        {
            case MappingType.SingleToSingle:
                {
                    item.GetConstructorValue(1, out var sp);
                    item.GetConstructorValue(2, out var tp);
                    mi.SourceName = [sp!.ToString()];
                    mi.SourceProp = [.. sourceProperties.Where(p => mi.SourceName.Contains(p.Name))];
                    mi.TargetName = [tp!.ToString()];
                    mi.TargetProp = [.. targetProperties.Where(p => mi.TargetName.Contains(p.Name))];
                    mi.MappingType = MappingType.SingleToSingle;
                    break;
                }
            case MappingType.SingleToMulti:
                {
                    item.GetConstructorValue(1, out var sp);
                    item.GetConstructorValues(2, out var tps);
                    mi.SourceName = [sp!.ToString()];
                    mi.SourceProp = [.. sourceProperties.Where(p => mi.SourceName.Contains(p.Name))];
                    mi.TargetName = [.. tps.Select(tp => tp!.ToString())];
                    mi.TargetProp = [.. targetProperties.Where(p => mi.TargetName.Contains(p.Name))];
                    mi.MappingType = MappingType.SingleToMulti;
                    break;
                }
            case MappingType.MultiToSingle:
                {
                    item.GetConstructorValues(1, out var sps);
                    item.GetConstructorValue(2, out var tp);
                    mi.SourceName = [.. sps.Select(sp => sp!.ToString())];
                    mi.SourceProp = [.. sourceProperties.Where(p => mi.SourceName.Contains(p.Name))];
                    mi.TargetName = [tp!.ToString()];
                    mi.TargetProp = [.. targetProperties.Where(p => mi.TargetName.Contains(p.Name))];
                    mi.MappingType = MappingType.MultiToSingle;
                    break;
                }
            default:
                throw new Exception();
        }

        if (item.GetNamedValue("By", out var by))
        {
            var methodName = by!.ToString();
            var error = HandleByMethod(declared, methodName, mi, location);
            if (error is not null)
            {
                return error;
            }
        }

        if (type != MappingType.SingleToSingle && mi.ForwardBy is null)
        {
            return DiagnosticDefinitions.AGM00006(location);
        }
        maps.Add(mi);

        return default;
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
    private static (AttributeData[] AttrDatas, Diagnostic? Error) GetSpecificDatas(INamedTypeSymbol type, INamedTypeSymbol matchSymbol)
    {
        var attrs = type.GetAttributes(GenMapBetweenAttributeFullName);
        List<AttributeData> result = [];
        foreach (var item in attrs)
        {
            if (item.ConstructorArguments.Length == 2)
            {
                return ([], DiagnosticDefinitions.AGM00004(type.Locations.FirstOrDefault()));
            }

            item.GetConstructorValue(0, out var t);
            var symbol = (INamedTypeSymbol)t!;
            if (EqualityComparer<INamedTypeSymbol>.Default.Equals(symbol, matchSymbol))
            {
                result.Add(item);
            }
        }

        return ([.. result], null);
    }
    // 如果是定义在方法上的，应该检查第一和第二个参数，如果是定义在类上的，应该检查第二和第三个参数
    private static MappingType CheckMapType(AttributeData data, int offset)
    {
        var s = data.ConstructorArguments[offset + 0];
        var t = data.ConstructorArguments[offset + 1];
        if (s.Type is IArrayTypeSymbol)
        {
            return MappingType.MultiToSingle;
        }
        else if (t.Type is IArrayTypeSymbol)
        {
            return MappingType.SingleToMulti;
        }
        else
        {
            return MappingType.SingleToSingle;
        }
    }

    private static Diagnostic? HandleByMethod(INamedTypeSymbol declared
        , string methodName
        , MapInfo mi
        , Location? location)
    {
        var methods = declared.GetAllMembers(_ => true).Where(i => i.Kind == SymbolKind.Method && i.Name == methodName).Cast<IMethodSymbol>().ToArray();
        if (methods.Length == 0)
        {
            return DiagnosticDefinitions.AGM00003(location);
        }

        var spts = mi.SourceProp.Select(p => p.Type).ToArray();
        var tpts = mi.TargetProp.Select(p => p.Type).ToArray();

        #region 正向映射

        var forward = methods.FirstOrDefault(m => CheckParameters(m, spts));
        if (!CheckMappingMethodReturnType(forward
                , declared.Locations.FirstOrDefault()
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
                , declared.Locations.FirstOrDefault()
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
