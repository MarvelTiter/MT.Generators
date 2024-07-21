using Generators.Shared;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
namespace Generators.Shared
{
    internal static class Extensions
    {
        public static IEnumerable<string> GetAttributeInitInfo(this ISymbol symbol, string targetGenerator, AttributeData data)
        {
            var allType = data.ConstructorArguments.FirstOrDefault().Values.Select(v => v.Value).Cast<INamedTypeSymbol>();
            foreach (var at in allType)
            {
                var parameters = symbol.GetAttributes(ConstAttributes.AttachArgumentAttribute)
                    .Where(a => a.ConstructorArguments[0].Value is INamedTypeSymbol tg
                    && a.ConstructorArguments[1].Value is INamedTypeSymbol ta
                    && tg.ToDisplayString() == targetGenerator
                    && SymbolEqualityComparer.Default.Equals(at, ta));
                var initParams = new Dictionary<string, object>();
                foreach (var item in parameters)
                {
                    var name = item.ConstructorArguments[2].Value as string;
                    var val = item.ConstructorArguments[3].Value;
                    if (val != null)
                        initParams.Add(name!, val);
                }
                yield return ConstructAttributeString(at, initParams);
            }
        }

        private static string ConstructAttributeString(INamedTypeSymbol targetAttribute, Dictionary<string, object> values)
        {
           var paramString = string.Join(", ", values.Select(kv => $"{kv.Key} = {(kv.Value.GetType() == typeof(string) ? $"\"{kv.Value}\"" : $"{kv.Value}")}"));

            return $"""
                {targetAttribute.ToDisplayString()}{(values.Count > 0 ? $"({paramString})" : "")}
                """;
        }
    }
}
