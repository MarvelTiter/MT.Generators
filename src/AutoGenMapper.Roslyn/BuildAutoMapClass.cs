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


            if (GetPropertyValue(context, prop, out var value, out var fromName, out var checkNull))
            {
                if (checkNull)
                {
                    var check = IfStatement.Default
                        .If($"this.{fromName} is not null")
                        .AddStatement($"_result_gen.{prop.Name} = {value}");
                    statements.Add(check);
                }
                else
                {
                    statements.Add($"_result_gen.{prop.Name} = {value}");
                }
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

    private static bool GetPropertyValue(GenMapperContext context
        , IPropertySymbol prop
        , out string? value
        , out string? fromName
        , out bool checkNull)
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
                fromName = null;
                checkNull = false;
                return true;
            }
            else if (!customTrans.From.IsNullOrEmpty())
            {
                fromName = customTrans.From;
                value = HandleComplexProperty(prop, customTrans.Target, customTrans.From!, out checkNull);
                return true;
            }
        }

        var p = context.SourceProperties.FirstOrDefault(p => p.Name == prop.Name);
        if (p != null)
        {
            fromName = prop.Name;
            value = HandleComplexProperty(prop, prop.ContainingType, prop.Name, out checkNull);
            return true;
        }
        value = null;
        fromName = null;
        checkNull = false;
        return false;
    }

    private static bool IsFromMapableObject(ITypeSymbol target, string name)
    {
        var prop = target.GetMembers().FirstOrDefault(m => m.Name == name) as IPropertySymbol;
        return prop?.Type.HasAttribute(AutoMapperGenerator.GenMapperAttributeFullName) == true;
    }

    private static string HandleComplexProperty(IPropertySymbol prop, ITypeSymbol fromTarget, string fromName, out bool checkNull)
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
            if (et.HasAttribute(AutoMapperGenerator.GenMapperAttributeFullName))
            {
                var na = prop.Type.NullableAnnotation == NullableAnnotation.Annotated ? "?" : "";
                checkNull = true;
                return ($"""this.{fromName}{na}.Where(i => i is not null).Select(i => i.MapTo<{et.ToDisplayString()}>("{et.MetadataName}")).{fin}""");
            }
            else
            {
                checkNull = false;
                return $"this.{fromName}";
            }
        }
        else if (IsFromMapableObject(fromTarget, fromName))
        {
            var na = prop.Type.NullableAnnotation == NullableAnnotation.Annotated ? "?" : "";
            checkNull = true;
            return $"""this.{fromName}{na}.MapTo<{prop.Type.ToDisplayString()}>("{prop.Type.MetadataName}")""";
        }
        else
        {
            checkNull = false;
            return $"this.{fromName}";
        }
    }
}