using Generators.Shared.Builder;

namespace Generators.Shared;

internal static class PropertyBuilderExtensions
{
    public static PropertyBuilder PropertyName(this PropertyBuilder builder, string name)
    {
        builder.Name = name;
        return builder;
    }
    public static PropertyBuilder Readonly(this PropertyBuilder builder)
    {
        builder.CanRead = true;
        builder.CanWrite = false;
        return builder;
    }

    public static PropertyBuilder Writeonly(this PropertyBuilder builder)
    {
        builder.CanRead = false;
        builder.CanWrite = true;
        return builder;
    }    

    public static PropertyBuilder Lambda(this PropertyBuilder builder, string body)
    {
        builder.IsLambdaBody = true;
        builder.Readonly();
        builder.InitializeWith(body);
        return builder;
    }
}
