using System.Collections.Generic;
using System.Linq;
using Generators.Shared;
using Generators.Shared.Builder;
using Microsoft.CodeAnalysis;

namespace AutoGenMapperGenerator;

public static partial class BuildAutoMapClass
{
    internal static MethodBuilder GenerateMapFromMethod(INamedTypeSymbol sourceType, MapperTarget context)
    {
        var statements = new List<Statement>();
        const string SOURCE_OBJECT = "_source_gen";
        //const string TARGET_OBJECT = "_target_gen";
        foreach (var item in context.Maps)
        {
            if (item.MappingType == MappingType.SingleToSingle)
            {
                var line = HandleReverseSingleToSingle(item, out var extra);
                if (extra is not null)
                    statements.Add(extra);
                statements.Add(line);
            }
            else if (item.MappingType == MappingType.MultiToSingle)
            {
                if (!item.CanReverse) continue;
                var invoker = item.ReverseBy!.TryGetMethodInvoker(sourceType);
                var sourceParam = item.TargetProp[0].Name;
                var tempArray = $"_{sourceParam}_arr_gen";
                var line = $"var {tempArray} = {invoker}.{item.ReverseBy!.Name}({string.Join(", ", $"{SOURCE_OBJECT}.{sourceParam}")})";
                statements.Add(line);
                //var checkArrResult = IfStatement.Default.If($"{tempArray} is not null");
                for (int i = 0; i < item.SourceProp.Count; i++)
                {
                    var tar = item.SourceProp[i];
                    statements.Add($"this.{tar.Name} = {tempArray}.Item{i + 1}");
                }
                //statements.Add(checkArrResult);
            }
            else if (item.MappingType == MappingType.SingleToMulti)
            {
                if (!item.CanReverse) continue;
                var invoker = item.ReverseBy!.TryGetMethodInvoker(sourceType);
                var targetName = item.SourceName.First();
                var pnames = string.Join(", ", item.TargetName.Select(s => $"{SOURCE_OBJECT}.{s}"));
                var line = $"this.{targetName} = {invoker}.{item.ReverseBy!.Name}({pnames})";
                statements.Add(line);
            }
        }

        var builder = MethodBuilder.Default
            .MethodName($"MapFrom{context.TargetType.Name}")
            .AddParameter($"{context.TargetType.ToDisplayString()} {SOURCE_OBJECT}")
            .AddGeneratedCodeAttribute(typeof(AutoMapperGenerator))
            .AddBody([.. statements]);

        return builder;

        Statement HandleReverseSingleToSingle(MapInfo item, out Statement? extra)
        {
            Statement line;
            extra = null;
            IPropertySymbol sp = item.SourceProp.First();
            IPropertySymbol tp = item.TargetProp.First();
            if (item.ReverseBy is not null && item.CanReverse)
            {
                var invoker = item.ReverseBy.IsStatic ? item.ReverseBy.ReceiverType?.ToDisplayString() ?? item.ReverseBy.ContainingType?.ToDisplayString() : "this";
                // 使用自定义映射
                line = $"this.{sp.Name} = {invoker}.{item.ReverseBy.Name}({SOURCE_OBJECT}.{tp.Name})";
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
                    if (sourceElement.HasAttribute(Helper.GenMapperAttributeFullName))
                    {
                        var na = sp.Type.NullableAnnotation == NullableAnnotation.Annotated ? "?" : "";
                        var localFuncName = $"Map_{sourceElement.Name}_From_{targetElement.Name}";
                        line = $"""
                                this.{sp.Name} = {SOURCE_OBJECT}.{tp.Name}{na}.Where(i => i is not null).Select({localFuncName}).{fin}
                                """;
                        extra = LocalFunction.Default.MethodName($"Map_{sourceElement.Name}_From_{targetElement.Name}")
                            .AddParameters($"{targetElement.Name} tar")
                            .Return(sourceElement.ToDisplayString())
                            .AddBody(
                                $"var r = new {sourceElement.ToDisplayString()}()",
                                "r.MapFrom(tar)",
                                "return r"
                            );
                    }
                    else
                    {
                        line = $"this.{tp.Name} = {SOURCE_OBJECT}.{sp.Name}";
                    }
                }
                else if (sp.Type.TypeKind == TypeKind.Class && sp.Type.SpecialType == SpecialType.None)
                {
                    // 属性是自定义类
                    if (sp.IsMapableObject())
                    {
                        var na = sp.Type.NullableAnnotation == NullableAnnotation.Annotated ? "?" : "";
                        line = $"""
                                this.{sp.Name}{na}.MapFrom({SOURCE_OBJECT}.{tp.Name})
                                """;
                    }
                    else
                    {
                        line = $"this.{sp.Name} = {SOURCE_OBJECT}.{tp.Name}";
                    }
                }
                else
                {
                    if (EqualityComparer<ITypeSymbol>.Default.Equals(sp.Type, tp.Type))
                    {
                        line = $"this.{sp.Name} = {SOURCE_OBJECT}.{tp.Name}";
                    }
                    else
                    {
                        // 处理类型转换
                        if (sp.Type.SpecialType == SpecialType.System_String)
                        {
                            line = $"this.{sp.Name} = {SOURCE_OBJECT}.{tp.Name}.ToString()";
                        }
                        else if (sp.Type.GetMembers().FirstOrDefault(m => m.Name == "TryParse") is IMethodSymbol tryParse
                                 && tp.Type.SpecialType == SpecialType.System_String)
                        {
                            line = IfStatement.Default.If($"{sp.Type.ToDisplayString()}.{tryParse.Name}({SOURCE_OBJECT}.{tp.Name}, out var _{sp.Name}_out_gen)")
                                .AddStatement($"this.{sp.Name} = _{sp.Name}_out_gen");
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