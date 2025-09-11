using Microsoft.CodeAnalysis;

namespace AutoInjectGenerator
{
    internal static class ExHelpers
    {
        /// <summary>
        /// symbol 是否可以作为 other的子类
        /// </summary>
        /// <param name="symbol"></param>
        /// <param name="other"></param>
        /// <returns></returns>
        public static bool IsSubClassOf(this INamedTypeSymbol symbol, INamedTypeSymbol other)
        {
            if (SymbolEqualityComparer.Default.Equals(symbol.BaseType, other))
            {
                return true;
            }
            else
            {
                if (symbol.BaseType is null)
                {
                    return false;
                }
                return symbol.BaseType.IsSubClassOf(other);
            }
        }
    }
}