using Generators.Shared.Builder;
using Generators.Shared;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Microsoft.CodeAnalysis.Text;

namespace AutoGenMapperGenerator;

[Generator(LanguageNames.CSharp)]
public class AutoMapFromDictionaryGenerator : IIncrementalGenerator
{
    public const string MapperContext = "AutoGenMapperGenerator.StaticMapperContext";
    public const string MapperFromDic = "AutoGenMapperGenerator.GenMapperFromDictionaryAttribute";
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(ctx =>
        {
            ctx.AddSource("Mapper.g.cs", SourceText.From("""
                using System;
                namespace AutoGenMapperGenerator;
                /// <summary>
                /// 拓展方法位置
                /// </summary>
                [StaticMapperContext]
                public static partial class Mapper
                {
                    private static T InternalConvert<T>(object value)
                    {
                        if (value is T t)
                        {
                            return t;
                        }
                        var targetType = typeof(T);
                        return (T)Convert.ChangeType(value, Nullable.GetUnderlyingType(targetType) ?? targetType);
                    }
                }
                """, Encoding.UTF8));
        });

        var map = context.SyntaxProvider.CreateSyntaxProvider(
     static (node, _) => node is ClassDeclarationSyntax @class && @class.Identifier.ToString() == "Mapper"
            , static (ctx, _) => ctx
            );
        context.RegisterSourceOutput(map, GenMapperExtension);
    }
    private static void GenMapperExtension(SourceProductionContext context, GeneratorSyntaxContext source)
    {
        var syntax = source.Node as ClassDeclarationSyntax;
        var sss = syntax?.AttributeLists.SelectMany(al => al.Attributes.Select(a => a.ToFullString()));
        var targets = source.SemanticModel.Compilation.GetAllSymbols(MapperFromDic);
        var ns = NamespaceBuilder.Default.Namespace("AutoGenMapperGenerator").FileScoped();
        var cb = ClassBuilder.Default.Modifiers("partial").ClassName("Mapper");
        List<MethodBuilder> methods = [];
        var method = MethodBuilder.Default.MethodName("MapTo")
            .Modifiers("public static")
            .ReturnType("object")
            .AddParameter("this global::System.Collections.Generic.IDictionary<string, object> source");
        foreach (var item in targets)
        {
            object?[] ctorParams = [];
            item.GetAttribute(MapperFromDic, out var attr);
            attr?.GetConstructorValues(0, out ctorParams);
            var typeName = item.ToDisplayString();
            var m = MethodBuilder.Default.MethodName($"MapTo{item.Name}")
                .Modifiers("public static")
                .ReturnType(typeName)
                .AddParameter("this global::System.Collections.Generic.IDictionary<string, object> source");

            var props = item.GetProperties().ToArray();
            List<Statement> body = [];
            var cps = props.Where(p => ctorParams.Contains(p.Name)).Select(ConvertType).Join(", ");
            body.Add($"var _val_gen = new {typeName}({cps})");
            foreach (var p in props)
            {
                if (ctorParams.Contains(p.Name)) continue;
                body.Add($"_val_gen.{p.Name} = {ConvertType(p)}");
            }
            body.Add("return _val_gen");
            m.AddBody([.. body]);
            cb.AddMembers(m);
        }

        var codeFile = CodeFile.New("StaticMapper.g.cs").AddMembers(ns.AddMembers(cb));

#if DEBUG
        var ss = codeFile.ToString();
#endif

        context.AddSource(codeFile);
    }

    private static string ConvertType(IPropertySymbol p)
    {
        return $"InternalConvert<{p.Type.ToDisplayString()}>(source[\"{p.Name}\"])";
    }
}
