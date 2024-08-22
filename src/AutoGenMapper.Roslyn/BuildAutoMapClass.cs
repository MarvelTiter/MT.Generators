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
            $"var result = new {context.TargetType.ToDisplayString()}({string.Join(", ", context.ConstructorParameters)})"
        };
        //Debugger.Launch();
        List<string> solved = [];
        foreach (var mapTo in context.Tos)
        {
            if (!EqualityComparer<INamedTypeSymbol>.Default.Equals(context.TargetType, mapTo.Target))
            {
                continue;
            }

            if (mapTo.By != null)
            {
                statements.Add($"this.{mapTo.By.Name}(result)");
            }
            else if (!mapTo.To.IsNullOrEmpty())
            {
                statements.Add($"result.{mapTo.To} = this.{mapTo.From}");
                solved.Add(mapTo.To!);
            }
        }

        foreach (var prop in context.TargetProperties)
        {
            if (solved.Contains(prop.Name))
            {
                continue;
            }
            if (prop.IsReadOnly)
            {
                continue;
            }
            if (GetPropertyValue(context, prop, out var value))
            {
                statements.Add($"result.{prop.Name} = {value}");
            }
        }

        statements.Add($"return result;");

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
                    value = $"result.{tranMethod.Name}(this)";
                }
                return true;
            }
            else if (!customTrans.From.IsNullOrEmpty())
            {
                value = $"this.{customTrans.From}";
                return true;
            }
        }

        var p = context.SourceProperties.FirstOrDefault(p => p.Name == prop.Name);
        if (p != null)
        {
            value = $"this.{p.Name}";
            return true;
        }
        value = null;
        return false;
    }
}