using Microsoft.CodeAnalysis;

namespace AutoWasmApiGenerator.Extensions;

internal static class MethodCheckerExtensions
{
    public static bool HasTryParseMethod(this ITypeSymbol returnType)
    {
        foreach (var method in returnType.GetMembers("TryParse").OfType<IMethodSymbol>())
        {
            var match = method is { Parameters.Length: 2, ReturnType.Name: "Boolean" };
            if (match) return true;
        }

        return false;
    }
}