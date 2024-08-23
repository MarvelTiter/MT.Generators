using Generators.Shared.Builder;

namespace Generators.Shared;

internal static class ClassBuilderExtensions
{
    public static ClassBuilder ClassName(this ClassBuilder builder, string className)
    {
        builder.Name = className;
        return builder;
    }

    public static ClassBuilder MakeRecord(this ClassBuilder builder)
    {
        builder.ClassType = "record";
        return builder;
    }

    public static ClassBuilder BaseType(this ClassBuilder builder, string baseType)
    {
        builder.BaseType = baseType;
        return builder;
    }

    public static ClassBuilder Interface(this ClassBuilder builder, params string[] interfaces)
    {
        foreach (var i in interfaces)
        {
            builder.Interfaces.Add(i);
        }
        return builder;
    }
}
