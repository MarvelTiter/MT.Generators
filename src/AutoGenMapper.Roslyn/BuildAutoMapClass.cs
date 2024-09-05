using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Generators.Shared;
using Generators.Shared.Builder;
using Microsoft.CodeAnalysis;

namespace AutoGenMapperGenerator;

public static class BuildAutoMapClass
{
    internal static MethodBuilder GenerateInterfaceMethod(GenMapperContext[] mapToMethods)
    {
        var statements = new List<Statement>();
        if (mapToMethods.Length == 1)
        {
            statements.Add($"return MapTo{mapToMethods[0].TargetType!.Name}()");
        }
        else
        {
            statements.Add($"if (string.IsNullOrEmpty(target))");
            statements.Add("""    throw new ArgumentNullException(nameof(target), "存在多个目标类型，请指定目标类型，推荐使用nameof(TargetType)")""");
            foreach (var method in mapToMethods)
            {
                statements.Add($"if (target == nameof({method.TargetType!.Name}))");
                statements.Add($"   return MapTo{method.TargetType.Name}();");
            }
            statements.Add("""throw new ArgumentException("未找到指定目标类型的映射方法");""");
        }
        var m = MethodBuilder.Default
            .MethodName("MapTo")
            .ReturnType("object?")
            .AddParameter("string? target = null")
            .AddGeneratedCodeAttribute(typeof(AutoMapperGenerator))
            .AddBody([.. statements]);
        return m;
    }
    internal static MethodBuilder GenerateMethod(GenMapperContext context)
    {
        var statements = new List<Statement>()
        {
            $"var _result_gen = new {context.TargetType.ToDisplayString()}({string.Join(", ", context.ConstructorParameters)})"
        };
        List<string> solved = [];
        foreach (var mapTo in context.Tos)
        {
            if (!EqualityComparer<INamedTypeSymbol>.Default.Equals(context.TargetType, mapTo.Target))
            {
                continue;
            }

            if (mapTo.By != null)
            {
                statements.Add($"this.{mapTo.By.Name}(_result_gen)");
            }
            else if (!mapTo.To.IsNullOrEmpty())
            {
                statements.Add($"_result_gen.{mapTo.To} = this.{mapTo.From}");
                solved.Add(mapTo.To!);
            }
        }

        foreach (var prop in context.TargetProperties)
        {
            if (solved.Contains(prop.Name) || context.ConstructorParameters.Contains(prop.Name))
            {
                continue;
            }
            //var mdis = prop.mod
            if (prop.IsReadOnly || prop.DeclaredAccessibility != Accessibility.Public)
            {
                continue;
            }


            if (GetPropertyValue(context, prop, out var value))
            {
                statements.Add($"_result_gen.{prop.Name} = {value}");
            }
        }

        statements.Add($"return _result_gen;");

        var builder = MethodBuilder.Default
            .MethodName($"MapTo{context.TargetType.Name}")
            .ReturnType(context.TargetType.ToDisplayString())
            .AddGeneratedCodeAttribute(typeof(AutoMapperGenerator))
            .AddBody([.. statements]);

        return builder;
    }

    private static bool GetPropertyValue(GenMapperContext context, IPropertySymbol prop, out string? value)
    {
        var customTrans = context.Froms.FirstOrDefault(f =>
        EqualityComparer<INamedTypeSymbol>.Default.Equals(f.Target, context.SourceType)
        && f.To == prop.Name);
        if (customTrans != null)
        {
            var tranMethod = customTrans.By;
            if (tranMethod != null)
            {
                if (tranMethod.IsStatic)
                {
                    value = $"{tranMethod.ReceiverType?.ToDisplayString() ?? tranMethod.ContainingType?.ToDisplayString()}.{tranMethod.Name}(this)";
                }
                else
                {
                    value = $"_result_gen.{tranMethod.Name}(this)";
                }
                return true;
            }
            else if (!customTrans.From.IsNullOrEmpty())
            {
                //if (prop.Type.HasInterfaceAll("System.Collections.IEnumerable") && prop.Type.SpecialType == SpecialType.None)
                //{
                //    ITypeSymbol? et = null;
                //    var fin = "";
                //    if (prop.Type is IArrayTypeSymbol at)
                //    {
                //        et = at.ElementType;
                //        fin = "ToArray()";
                //    }
                //    else
                //    {
                //        et = prop.Type.GetGenericTypes().First();
                //        fin = "ToList()";
                //    }
                //    if (et.HasInterface(AutoMapperGenerator.GenMapableInterface))
                //    {
                //        var na = prop.Type.NullableAnnotation == NullableAnnotation.Annotated ? "?" : "";
                //        value = ($"""this.{customTrans.From}{na}.Select(i => i.MapTo<{et.ToDisplayString()}>("{et.MetadataName}")).{fin}""");
                //    }
                //    else
                //    {
                //        value = $"this.{customTrans.From}";
                //    }
                //}
                //else if (IsFromMapableObject(customTrans.Target, customTrans.From))
                //{
                //    value = $"""this.{customTrans.From}.MapTo<{prop.Type.ToDisplayString()}>("{prop.Type.MetadataName}")""";
                //}
                //else
                //{
                //    value = $"this.{customTrans.From}";
                //}
                value = HandleComplexProperty(prop, customTrans.Target, customTrans.From!);
                return true;
            }
        }

        var p = context.SourceProperties.FirstOrDefault(p => p.Name == prop.Name);
        if (p != null)
        {
            //if (prop.Type.HasInterfaceAll("System.Collections.IEnumerable") && prop.Type.SpecialType == SpecialType.None)
            //{
            //    ITypeSymbol? et = null;
            //    var fin = "";
            //    if (prop.Type is IArrayTypeSymbol at)
            //    {
            //        et = at.ElementType;
            //        fin = "ToArray()";
            //    }
            //    else
            //    {
            //        et = prop.Type.GetGenericTypes().First();
            //        fin = "ToList()";
            //    }
            //    if (et.HasInterface(AutoMapperGenerator.GenMapableInterface))
            //    {
            //        var na = prop.Type.NullableAnnotation == NullableAnnotation.Annotated ? "?" : "";
            //        value = ($"""this.{p.Name}{na}.Select(i => i.MapTo<{et.ToDisplayString()}>("{et.MetadataName}")).{fin}""");
            //    }
            //    else if (IsFromMapableObject(prop.Type, prop.Name))
            //    {
            //        value = $"""this.{prop.Name}.MapTo<{prop.Type.ToDisplayString()}>("{prop.Type.MetadataName}")""";
            //    }
            //    else
            //    {
            //        value = $"this.{p.Name}";
            //    }
            //}
            //else
            //{
            //    value = $"this.{p.Name}";
            //}
            value = HandleComplexProperty(prop, prop.Type, prop.Name);
            return true;
        }
        value = null;
        return false;
    }

    private static bool IsFromMapableObject(ITypeSymbol target)
    {
        return target.HasInterface(AutoMapperGenerator.GenMapableInterface);
    }

    private static string HandleComplexProperty(IPropertySymbol prop, ITypeSymbol fromTarget, string fromName)
    {
        if (prop.Type.HasInterfaceAll("System.Collections.IEnumerable") && prop.Type.SpecialType == SpecialType.None)
        {
            ITypeSymbol? et = null;
            var fin = "";
            if (prop.Type is IArrayTypeSymbol at)
            {
                et = at.ElementType;
                fin = "ToArray()";
            }
            else
            {
                et = prop.Type.GetGenericTypes().First();
                fin = "ToList()";
            }
            if (et.HasInterface(AutoMapperGenerator.GenMapableInterface))
            {
                var na = prop.Type.NullableAnnotation == NullableAnnotation.Annotated ? "?" : "";
                return ($"""this.{fromName}{na}.Select(i => i.MapTo<{et.ToDisplayString()}>("{et.MetadataName}")).{fin}""");
            }
            else
            {
                return $"this.{fromName}";
            }
        }
        else if (IsFromMapableObject(fromTarget))
        {
            var na = prop.Type.NullableAnnotation == NullableAnnotation.Annotated ? "?" : "";
            return $"""this.{fromName}{na}.MapTo<{prop.Type.ToDisplayString()}>("{prop.Type.MetadataName}")""";
        }
        else
        {
            return $"this.{fromName}";
        }
    }
}