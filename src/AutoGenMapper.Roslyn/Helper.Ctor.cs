using Generators.Shared;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AutoGenMapperGenerator;

internal partial class Helper
{
    public static List<MapperTarget> CollectMapTargets(ISymbol symbol)
    {
        try
        {
            List<MapperTarget> targets = [];
            if (symbol is INamedTypeSymbol source)
            {
                foreach (var a in source.GetAttributes(GenMapperAttributeFullName))
                {
                    // 获取具名的TargetType属性或者构造函数的第一个参数作为目标类型
                    if (!(a.GetNamedValue("TargetType", out var t) && t is INamedTypeSymbol target))
                    {
                        target = (a.ConstructorArguments.FirstOrDefault().Value as INamedTypeSymbol) ?? source;
                    }
                    var mapTarget = new MapperTarget(target);
                    // 处理构造函数
                    if (a?.ConstructorArguments.Length > 1)
                    {
                        // 指定了构造函数参数
                        mapTarget.ConstructorParameters =
                            [.. a.ConstructorArguments[1].Values.Where(c => c.Value != null).Select(c => c.Value!.ToString())];
                    }
                    else
                    {
                        var c = GetSpecificAttribute(source, target, GenMapConstructorAttributeFullName);
                        if (c?.ConstructorArguments.Length == 2)
                        {
                            // 指定了构造函数参数
                            mapTarget.ConstructorParameters =
                            [.. c.ConstructorArguments[1].Values.Where(c => c.Value != null).Select(c => c.Value!.ToString())];
                        }
                        else
                        {
                            TrySetDefaultCtor(source, mapTarget);
                        }
                    }


                    targets.Add(mapTarget);
                }
            }
            else if (symbol is IMethodSymbol method)
            {
                var (IsTask, HasReturn, ReturnType) = method.GetReturnTypeInfo();
                var methodSource = (INamedTypeSymbol)method.Parameters[0].Type;
                var target = (INamedTypeSymbol)ReturnType;
                var mapTarget = new MapperTarget(target);
                if (method.GetAttribute(GenMapConstructorAttributeFullName, out var c) && c!.ConstructorArguments.Length == 1)
                {
                    mapTarget.ConstructorParameters =
                    [.. c.ConstructorArguments[0].Values.Where(c => c.Value != null).Select(c => c.Value!.ToString())];
                }
                else
                {
                    TrySetDefaultCtor(methodSource, mapTarget);
                }
                targets.Add(mapTarget);
            }

            return targets;
        }
        catch (Exception)
        {
            throw;
        }

        static void TrySetDefaultCtor(INamedTypeSymbol source, MapperTarget mapTarget)
        {
            // 尝试自动获取构造函数参数
            var ctorSymbol = mapTarget.TargetType.GetMethods().FirstOrDefault(m => m.MethodKind == MethodKind.Constructor && m.Parameters.Length > 0);
            ctorSymbol ??= mapTarget.TargetType.GetMethods().FirstOrDefault(m => m.MethodKind == MethodKind.Constructor);
            if (ctorSymbol is null)
            {
                throw new Exception("AGM00011");
            }
            string[] ps = [.. ctorSymbol.Parameters.Select(p => source.GetAllMembers(_ => true).FirstOrDefault(s => string.Equals(p.Name, s.Name, StringComparison.OrdinalIgnoreCase))?.Name).Where(s => !string.IsNullOrEmpty(s))!];
            if (ps.Length != ctorSymbol.Parameters.Length)
            {
                throw new Exception("AGM00013");
            }
            mapTarget.ConstructorParameters = ps;
        }
    }

    private static void HandleConstructor(AttributeData? a
        , ISymbol entry
        , INamedTypeSymbol source
        , INamedTypeSymbol target
        , MapperTarget mapTarget)
    {
        if (a?.ConstructorArguments.Length > 1)
        {
            // 指定了构造函数参数
            mapTarget.ConstructorParameters =
                [.. a.ConstructorArguments[1].Values.Where(c => c.Value != null).Select(c => c.Value!.ToString())];
        }
        else if (entry is INamedTypeSymbol)
        {
            var c = GetSpecificAttribute(source, target, GenMapConstructorAttributeFullName);
            if (c?.ConstructorArguments.Length == 2)
            {
                // 指定了构造函数参数
                mapTarget.ConstructorParameters =
                [.. c.ConstructorArguments[1].Values.Where(c => c.Value != null).Select(c => c.Value!.ToString())];
            }
        }
        else if (entry is IMethodSymbol)
        {
            if (entry.GetAttribute(GenMapConstructorAttributeFullName, out var c) && c!.ConstructorArguments.Length == 1)
            {
                mapTarget.ConstructorParameters =
                [.. c.ConstructorArguments[0].Values.Where(c => c.Value != null).Select(c => c.Value!.ToString())];
            }
        }
        else
        {
            // 尝试自动获取构造函数参数
            var ctorSymbol = mapTarget.TargetType.GetMethods().FirstOrDefault(m => m.MethodKind == MethodKind.Constructor && m.Parameters.Length > 0);
            ctorSymbol ??= mapTarget.TargetType.GetMethods().FirstOrDefault(m => m.MethodKind == MethodKind.Constructor);
            if (ctorSymbol is null)
            {
                throw new Exception("AGM00011");
            }
            string[] ps = [.. ctorSymbol.Parameters.Select(p => source.GetAllMembers(_ => true).FirstOrDefault(s => string.Equals(p.Name, s.Name, StringComparison.OrdinalIgnoreCase))?.Name).Where(s => !string.IsNullOrEmpty(s))!];
            if (ps.Length != ctorSymbol.Parameters.Length)
            {
                throw new Exception("AGM00013");
            }
            mapTarget.ConstructorParameters = ps;
        }
    }
}
