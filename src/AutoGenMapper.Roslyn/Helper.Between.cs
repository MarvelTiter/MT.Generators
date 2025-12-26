using Generators.Shared;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AutoGenMapperGenerator;

internal partial class Helper
{
    public static IEnumerable<MapBetweenInfo> CollectSpecificBetweenInfo(ISymbol symbol)
    {
        var pos = DeclarePosition.Property;
        if (symbol is INamedTypeSymbol)
        {
            pos = DeclarePosition.Class;
        }
        else if (symbol is IMethodSymbol)
        {
            pos = DeclarePosition.Method;
        }
        foreach (var item in symbol.GetAttributes(GenMapBetweenAttributeFullName))
        {
            MapBetweenInfo info;
            if (pos == DeclarePosition.Class)
            {
                info = HandleClassDeclare(item);
            }
            else if (pos == DeclarePosition.Method && symbol is IMethodSymbol m)
            {
                info = HandleMethodDeclare(item, m);
            }
            else if (pos == DeclarePosition.Property && symbol is IPropertySymbol p)
            {
                info = HandlePropertyDeclare(item, p);
            }
            else
            {
                throw new InvalidOperationException();
            }
            item.GetNamedValue("By", out var by);
            info.By = by?.ToString();
            yield return info;
        }
        if (symbol is INamedTypeSymbol classSymbol)
        {
            foreach (var m in HandleClassProperties(classSymbol))
            {
                yield return m;
            }
        }
        else if (symbol is IMethodSymbol method)
        {
            classSymbol = (INamedTypeSymbol)method.Parameters[0].Type;
            foreach (var m in HandleClassProperties(classSymbol))
            {
                yield return m;
            }
        }


        static MapBetweenInfo HandleClassDeclare(AttributeData a)
        {

            if (a.ConstructorArguments.Length != 3)
            {
                throw new InvalidOperationException();
            }
            //if (a.GetConstructorValue(0, out var tar) && tar is INamedTypeSymbol symbol1)
            //{
            //    target = symbol1;
            //}
            a.GetConstructorValue(0, out var tar);
            INamedTypeSymbol target = (INamedTypeSymbol)tar!;

            var mapType = GetMapTypeAndValues(a, 1, out var sources, out var targets);
            return new(target)
            {
                Position = DeclarePosition.Class,
                MapType = mapType,
                Sources = sources,
                Targets = targets,
            };
        }

        static IEnumerable<MapBetweenInfo> HandleClassProperties(INamedTypeSymbol source)
        {
            var sourceProperties = source.GetAllMembers(_ => true).Where(i => i.Kind == SymbolKind.Property)
                .Cast<IPropertySymbol>().ToArray();
            foreach (var sourceProperty in sourceProperties)
            {
                foreach (var item in CollectSpecificBetweenInfo(sourceProperty))
                {
                    yield return item;
                }
            }
        }

        static MapBetweenInfo HandleMethodDeclare(AttributeData a, IMethodSymbol method)
        {
            var mapType = GetMapTypeAndValues(a, 0, out var sources, out var targets);
            var (IsTask, HasReturn, ReturnType) = method.GetReturnTypeInfo();
            return new((INamedTypeSymbol)ReturnType)
            {
                Position = DeclarePosition.Method,
                MapType = mapType,
                Sources = sources,
                Targets = targets,
            };
        }

        static MapBetweenInfo HandlePropertyDeclare(AttributeData a, IPropertySymbol prop)
        {
            if (a.ConstructorArguments.Length != 2)
            {
                throw new InvalidOperationException();
            }
            //if (a.GetConstructorValue(0, out var tar) && tar is INamedTypeSymbol symbol1)
            //{
            //    target = symbol1;
            //}
            a.GetConstructorValue(0, out var tar);
            a.GetConstructorValue(1, out var t);
            return new((INamedTypeSymbol)tar!)
            {
                Position = DeclarePosition.Property,
                MapType = MappingType.SingleToSingle,
                Sources = [prop.Name],
                Targets = [t!.ToString()],
            };
        }


    }

    private static MappingType GetMapTypeAndValues(AttributeData data, int offset
        , out string[] sources
        , out string[] targets)
    {
        var s = data.ConstructorArguments[offset + 0];
        var t = data.ConstructorArguments[offset + 1];
        if (s.Type is IArrayTypeSymbol)
        {
            data.GetConstructorValues(offset + 0, out var sps);
            data.GetConstructorValue(offset + 1, out var tp);
            sources = [.. sps.Select(sp => sp!.ToString())];
            targets = [tp!.ToString()];
            return MappingType.MultiToSingle;
        }
        else if (t.Type is IArrayTypeSymbol)
        {
            data.GetConstructorValue(offset + 0, out var sp);
            data.GetConstructorValues(offset + 1, out var tps);
            sources = [sp!.ToString()];
            targets = [.. tps.Select(tp => tp!.ToString())];
            return MappingType.SingleToMulti;
        }
        else
        {
            data.GetConstructorValue(offset + 0, out var sp);
            data.GetConstructorValue(offset + 1, out var tp);
            sources = [sp!.ToString()];
            targets = [tp!.ToString()];
            return MappingType.SingleToSingle;
        }
    }
}
