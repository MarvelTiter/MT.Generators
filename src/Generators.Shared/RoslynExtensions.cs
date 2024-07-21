﻿using Generators.Shared.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Generators.Shared
{
    internal static class RoslynExtensions
    {
        /// <summary>
        /// 获取指定了名称的参数的值
        /// </summary>
        /// <param name="a"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static object? GetNamedValue(this AttributeData? a, string key)
        {
            if (a == null) return null;
            var named = a.NamedArguments.FirstOrDefault(t => t.Key == key);
            return named.Value.Value;
        }
        /// <summary>
        /// 获取指定了名称的参数的值
        /// </summary>
        /// <param name="a"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static bool GetNamedValue(this AttributeData? a, string key, out object? value)
        {
            var t = GetNamedValue(a, key);
            value = t;
            return t != null;
        }
        /// <summary>
        /// 根据名称获取attribute的值
        /// </summary>
        /// <param name="symbol"></param>
        /// <param name="fullName"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public static bool GetAttribute(this ISymbol? symbol, string fullName, out AttributeData? data)
        {
            data = symbol?.GetAttributes().FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == fullName);
            return data != null;
        }
        /// <summary>
        /// 根据名称获取attribute的值
        /// </summary>
        /// <param name="symbol"></param>
        /// <param name="fullName"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public static IEnumerable<AttributeData> GetAttributes(this ISymbol? symbol, string fullName)
        {
            foreach (var item in symbol?.GetAttributes() ?? [])
            {
                if (item.AttributeClass?.ToDisplayString() == fullName)
                {
                    yield return item;
                }
            }
        }
        /// <summary>
        /// 根据名称判定是否拥有某个attribute
        /// </summary>
        /// <param name="symbol"></param>
        /// <param name="fullName"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public static bool HasAttribute(this ISymbol? symbol, string fullName)
        {
            return symbol?.GetAttributes().Any(a => a.AttributeClass?.ToDisplayString() == fullName) == true;
        }

        public static bool CheckDisableGenerator(this AnalyzerConfigOptionsProvider options, string key)
        {
            return options.GlobalOptions.TryGetValue($"build_property.{key}", out var value) && !string.IsNullOrEmpty(value);
        }

        public static string[] GetTargetUsings(this GeneratorAttributeSyntaxContext source)
        {
            if (source.TargetNode is
                {

                    Parent: NamespaceDeclarationSyntax
                    {
                        Usings: var nu,
                        Parent: CompilationUnitSyntax
                        {
                            Usings: var cnu
                        }
                    }
                }
                )
            {
                UsingDirectiveSyntax[] arr = [.. nu, .. cnu];
                return arr.Select(a => a.ToFullString().Replace("\n", "")).ToArray();
            }

            return [];
        }

        public static IEnumerable<(IMethodSymbol Symbol, AttributeData? AttrData)> GetAllMethodWithAttribute(this INamedTypeSymbol interfaceSymbol, string fullName, INamedTypeSymbol? classSymbol = null)
        {
            var all = interfaceSymbol.Interfaces.Insert(0, interfaceSymbol);
            foreach (var m in all)
            {
                foreach (var item in m.GetMembers().Where(m => m is IMethodSymbol).Cast<IMethodSymbol>())
                {
                    if (item.MethodKind == MethodKind.Constructor)
                    {
                        continue;
                    }

                    var classMethod = classSymbol?.GetMembers().FirstOrDefault(m => m.Name == item.Name);

                    if (!item.GetAttribute(fullName, out var a))
                    {
                        if (!classMethod.GetAttribute(fullName, out a))
                        {
                            a = null;
                        }
                    }
                    var method = m.IsGenericType ? item.ConstructedFrom : item;
                    yield return (method, a);
                }
            }
        }

        public static IEnumerable<ITypeSymbol> GetGenericTypes(this ITypeSymbol symbol)
        {
            if (symbol is INamedTypeSymbol { IsGenericType: true, TypeArguments: var types })
            {
                foreach (var t in types)
                {
                    yield return t;
                }
            }
            //else
            //{
            //    yield return symbol;
            //}
        }

        public static IEnumerable<TypeParameterInfo> GetTypeParameters(this ISymbol symbol)
        {
            IEnumerable<ITypeParameterSymbol> tpc = [];
            if (symbol is IMethodSymbol method)
            {
                tpc = method.TypeParameters;
            }
            else if (symbol is INamedTypeSymbol typeSymbol)
            {
                tpc = typeSymbol.TypeParameters;
            }
            else
            {
                yield break;
            }

            foreach (var tp in tpc)
            {
                List<string> cs = tp.ConstraintTypes.Select(t => t.Name).ToList();
                tp.HasNotNullConstraint.IsTrueThen(() => cs.Add("notnull"));
                tp.HasReferenceTypeConstraint.IsTrueThen(() => cs.Add("class"));
                tp.HasUnmanagedTypeConstraint.IsTrueThen(() => cs.Add("unmanaged "));
                tp.HasValueTypeConstraint.IsTrueThen(() => cs.Add("struct"));
                tp.HasConstructorConstraint.IsTrueThen(() => cs.Add("new()"));
                yield return new(tp.Name, [.. cs]);
            }
        }

        private static void IsTrueThen(this bool value, Action action)
        {
            if (value)
            {
                action();
            }
        }
    }
}
