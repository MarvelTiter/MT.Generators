using Generators.Shared;
using Generators.Shared.Builder;

namespace Generators.Shared;

internal static class NameSpaceBuilderExtensions
{
    public static NamespaceBuilder Namespace(this NamespaceBuilder builder, string @namespace)
    {
        builder.Namespace = @namespace;
        return builder;
    }
}
