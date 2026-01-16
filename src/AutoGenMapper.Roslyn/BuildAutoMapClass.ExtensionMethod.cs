using Generators.Shared;
using Generators.Shared.Builder;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;

namespace AutoGenMapperGenerator;

public static partial class BuildAutoMapClass
{
    internal static MethodBuilder GenerateExtensionMethod(MapperContext mapperContext, MapperTarget context)
    {
        var method = (IMethodSymbol)mapperContext.TargetSymbol;
        var sourceName = method.Parameters[0].Name;
        var statements = BuildMapToMethodStatements(TARGET_OBJECT, sourceName, mapperContext.SourceType, context, CustomActionHandler);

        var builder = MethodBuilder.Default
            .Partial(method)
            .AddGeneratedCodeAttribute(typeof(AutoMapperGenerator))
            .AddBody([.. statements]);

        return builder;


        IEnumerable<Statement> CustomActionHandler()
        {
            if (method.Parameters.Length > 1 && method.Parameters[1].Type.Name.Contains("Action"))
            {
                var p2 = method.Parameters[1];
                var ext = p2.Type as INamedTypeSymbol;
                if (ext?.DelegateInvokeMethod is { } custom && custom.Parameters.Length == 2)
                {
                    if (EqualityComparer<ITypeSymbol>.Default.Equals(custom.Parameters[0].Type, mapperContext.SourceType)
                        && EqualityComparer<ITypeSymbol>.Default.Equals(custom.Parameters[1].Type, context.TargetType))
                    {
                        yield return IfStatement.Default.If($"{p2.Name} is not null").AddStatement($"{p2.Name}.Invoke({sourceName}, {TARGET_OBJECT})");
                    }
                    else if (EqualityComparer<ITypeSymbol>.Default.Equals(custom.Parameters[1].Type, mapperContext.SourceType)
                        && EqualityComparer<ITypeSymbol>.Default.Equals(custom.Parameters[0].Type, context.TargetType))
                    {
                        yield return IfStatement.Default.If($"{p2.Name} is not null").AddStatement($"{p2.Name}.Invoke({TARGET_OBJECT}, {sourceName})");
                    }
                }
            }
        }
    }
}
