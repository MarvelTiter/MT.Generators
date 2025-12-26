using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Generators.Shared;
using Generators.Shared.Builder;
using Microsoft.CodeAnalysis;

namespace AutoGenMapperGenerator;

public static partial class BuildAutoMapClass
{
    internal static IEnumerable<MethodBuilder> GenerateInterfaceMethod(List<MapperTarget> contexts)
    {
        #region map to method
        {
            var statements = new List<Statement>();
            if (contexts.Count == 1)
            {
                statements.Add($"return MapTo{contexts[0].TargetType!.Name}()");
            }
            else
            {
                statements.Add($"if (string.IsNullOrEmpty(target))");
                statements.Add("""    throw new ArgumentNullException(nameof(target), "存在多个目标类型，请指定目标类型，推荐使用nameof(TargetType)")""");
                foreach (var method in contexts)
                {
                    statements.Add($"if (target == nameof({method.TargetType!.Name}))");
                    statements.Add($"   return MapTo{method.TargetType.Name}();");
                }
                statements.Add("""throw new ArgumentException("未找到指定目标类型的映射方法");""");
            }
            var mapTo = MethodBuilder.Default
                .MethodName("MapTo")
                .ReturnType("object?")
                .AddParameter("string? target = null")
                .AddGeneratedCodeAttribute(typeof(AutoMapperGenerator))
                .AddBody([.. statements]);
            yield return mapTo;
        }
        #endregion

        #region map from method
        {
            var statements = new List<Statement>();
            if (contexts.Count == 1)
            {
                var ctx = contexts[0];
                statements.Add(IfStatement.Default.If($"value is {ctx.TargetType.ToDisplayString()} source").AddStatement($"MapFrom{contexts[0].TargetType.Name}(source)"));
            }
            else
            {
                statements.Add(IfStatement.Default.If("value is null")
                    .AddStatement("return"));
                int i = 0;
                foreach (var ctx in contexts)
                {
                    var sn = $"source_{i}";
                    statements.Add(IfStatement.Default.If($"value is {ctx.TargetType.ToDisplayString()} {sn}")
                        .AddStatement($"MapFrom{ctx.TargetType.Name}({sn})", "return")); i++;
                }
                statements.Add("""throw new ArgumentException("未找到指定目标类型的映射方法");""");
            }
            var mapFrom = MethodBuilder.Default
                .MethodName("MapFrom")
                .AddParameter("object? value")
                .AddGeneratedCodeAttribute(typeof(AutoMapperGenerator))
                .AddBody([.. statements]);
            yield return mapFrom;

        }

        #endregion

    }
    static bool IsMapableObject(this IPropertySymbol target)
    {
        return target.Type.HasAttribute(AutoMapperGenerator.GenMapperAttributeFullName) == true;
    }
    static string TryGetMethodInvoker(this IMethodSymbol method, INamedTypeSymbol source)
    {
        //return method.IsStatic ?
        //    method.ReceiverType?.ToDisplayString() ?? method.ContainingType?.ToDisplayString()
        //    : null;
        return method.ContainingType.ToDisplayString();
    }

    internal static MethodBuilder GenerateMapToMethod(INamedTypeSymbol sourceType, MapperTarget context)
    {
        const string TARGET_OBJECT = "_result_gen";
        const string SOURCE_OBJECT = "this";
        var statements = new List<Statement>()
        {
            $"var {TARGET_OBJECT} = new {context.TargetType.ToDisplayString()}({string.Join(", ", context.ConstructorParameters)})"
        };

        foreach (var item in context.Maps)
        {
            if (item.MappingType == MappingType.SingleToSingle)
            {
                var line = HandleForwardSingleToSingle(item);
                statements.Add(line);
            }
            else if (item.MappingType == MappingType.MultiToSingle)
            {
                var invoker = item.ForwardBy!.TryGetMethodInvoker(sourceType);
                var targetName = item.TargetName.First();
                var pnames = string.Join(", ", item.SourceName.Select(s => $"{SOURCE_OBJECT}.{s}"));
                var line = $"{TARGET_OBJECT}.{targetName} = {invoker}.{item.ForwardBy!.Name}({pnames})";
                statements.Add(line);
            }
            else if (item.MappingType == MappingType.SingleToMulti)
            {
                var invoker = item.ForwardBy!.TryGetMethodInvoker(sourceType);
                var sourceParam = item.SourceName[0];
                var tempArray = $"_{sourceParam}_arr_gen";
                var line = $"var {tempArray} = {invoker}.{item.ForwardBy!.Name}({string.Join(", ", $"{SOURCE_OBJECT}.{sourceParam}")})";
                statements.Add(line);
                //var checkArrResult = IfStatement.Default.If($"{tempArray} is not null");
                for (int i = 0; i < item.TargetProp.Count; i++)
                {
                    var tar = item.TargetProp[i];
                    statements.Add($"{TARGET_OBJECT}.{tar.Name} = {tempArray}.Item{i + 1}");
                }
                //statements.Add(checkArrResult);
            }
        }

        statements.Add($"return {TARGET_OBJECT};");

        var builder = MethodBuilder.Default
            .MethodName($"MapTo{context.TargetType.Name}")
            //.Modifiers("private static")
            //.AddParameter($"{sourceType.ToDisplayString()} {SOURCE_OBJECT}")
            .ReturnType(context.TargetType.ToDisplayString())
            .AddGeneratedCodeAttribute(typeof(AutoMapperGenerator))
            .AddBody([.. statements]);

        return builder;

        Statement HandleForwardSingleToSingle(MapInfo item)
        {
            Statement line;
            IPropertySymbol sp = item.SourceProp.First();
            IPropertySymbol tp = item.TargetProp.First();
            if (item.ForwardBy is not null)
            {
                var invoker = item.ForwardBy.TryGetMethodInvoker(sourceType);
                // 使用自定义映射
                line = $"{TARGET_OBJECT}.{tp.Name} = {invoker}.{item.ForwardBy.Name}(this.{sp.Name})";
            }
            else
            {
                if (sp.Type.HasInterfaceAll("System.Collections.IEnumerable")
                    && sp.Type.SpecialType == SpecialType.None
                    && tp.Type.HasInterface("System.Collections.IEnumerable")
                    && tp.Type.SpecialType == SpecialType.None)
                {
                    // 属性是数组或者其他可迭代对象
                    var sourceElement = sp.Type.GetElementType();
                    var targetElement = tp.Type.GetElementType();
                    var fin = sp.Type is IArrayTypeSymbol ? "ToArray()" : "ToList()";
                    if (sourceElement.HasAttribute(AutoMapperGenerator.GenMapperAttributeFullName))
                    {
                        var na = sp.Type.NullableAnnotation == NullableAnnotation.Annotated ? "?" : "";
                        line = $"""
{TARGET_OBJECT}.{tp.Name} = this.{sp.Name}{na}.Where(i => i is not null).Select(i => i.MapTo<{targetElement.ToDisplayString()}>("{targetElement.MetadataName}")).{fin}
""";
                    }
                    else
                    {
                        line = $"{TARGET_OBJECT}.{tp.Name} = {SOURCE_OBJECT}.{sp.Name}";
                    }
                }
                else if (sp.Type.TypeKind == TypeKind.Class && sp.Type.SpecialType == SpecialType.None)
                {
                    // 属性是自定义类
                    if (sp.IsMapableObject())
                    {
                        var na = sp.Type.NullableAnnotation == NullableAnnotation.Annotated ? "?" : "";
                        line = $"""
{TARGET_OBJECT}.{tp.Name} = {SOURCE_OBJECT}.{sp.Name}{na}.MapTo<{tp.Type.ToDisplayString()}>("{tp.Type.MetadataName}")
""";
                    }
                    else
                    {
                        line = $"{TARGET_OBJECT}.{tp.Name} = {SOURCE_OBJECT}.{sp.Name}";
                    }
                }
                else
                {
                    if (EqualityComparer<ITypeSymbol>.Default.Equals(sp.Type, tp.Type))
                    {
                        line = $"{TARGET_OBJECT}.{tp.Name} = {SOURCE_OBJECT}.{sp.Name}";
                    }
                    else
                    {
                        // 处理类型转换
                        if (tp.Type.SpecialType == SpecialType.System_String)
                        {
                            line = $"{TARGET_OBJECT}.{tp.Name} = {SOURCE_OBJECT}.{sp.Name}.ToString()";
                        }
                        else if (tp.Type.GetMembers().FirstOrDefault(m => m.Name == "TryParse") is IMethodSymbol tryParse
                            && sp.Type.SpecialType == SpecialType.System_String)
                        {
                            line = IfStatement.Default.If($"{tp.Type.ToDisplayString()}.{tryParse.Name}({SOURCE_OBJECT}.{sp.Name}.ToString(), out var _{tp.Name}_out_gen)")
                                .AddStatement($"{TARGET_OBJECT}.{tp.Name} = _{tp.Name}_out_gen");
                        }
                        else
                        {
                            line = $"""
                                    throw new AutoGenMapperGenerator.AutoGenMapperException("{sourceType.ToDisplayString()}.{sp.Name}和{context.TargetType.ToDisplayString()}.{tp.Name}尝试自动类型转换失败，请自定义{sp.Type.ToDisplayString()}和{tp.Type.ToDisplayString()}之间的转换")
                                    """;
                        }
                    }
                }
            }

            return line;
        }
    }
}