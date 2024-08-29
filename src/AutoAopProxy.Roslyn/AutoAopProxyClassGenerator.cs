using Generators.Shared;
using Generators.Shared.Builder;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AutoAopProxyGenerator;

[Generator(LanguageNames.CSharp)]
public class AutoAopProxyClassGenerator : IIncrementalGenerator
{
    public const string Aspectable = "AutoAopProxyGenerator.GenAspectProxyAttribute";
    public const string AspectHandler = "AutoAopProxyGenerator.AddAspectHandlerAttribute";
    public const string IgnoreAspect = "AutoAopProxyGenerator.IgnoreAspectAttribute";
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var source = context.SyntaxProvider.ForAttributeWithMetadataName(
            Aspectable
            , static (node, _) => node is ClassDeclarationSyntax
            , static (ctx, _) => ctx);
        context.RegisterSourceOutput(source, static (context, source) =>
        {
            var targetSymbol = (INamedTypeSymbol)source.TargetSymbol;
            //INamedTypeSymbol[] needToCheckSymbol = [targetSymbol, .. targetSymbol.AllInterfaces];


            var allInterfaces = targetSymbol.AllInterfaces.Where(a =>
            {
                // 接口上标注了AddAspectHandlerAttribute，或者接口中有方法标注了AddAspectHandlerAttribute
                return a.HasAttribute(AspectHandler) || a.GetMethods().Where(m => m.MethodKind != MethodKind.Constructor).Any(m => m.HasAttribute(AspectHandler));
            }).ToArray();

            //#region 检查Attribute标注是否合法
            //bool pass = true;
            //foreach (var i in allInterfaces)
            //{
            //    if (!pass)
            //    {
            //        break;
            //    }
            //    var allAttribute = i.GetAttributes(AspectHandler).Concat(i.GetMethods().Where(m => m.MethodKind != MethodKind.Constructor).SelectMany(m => m.GetAttributes(AspectHandler)));
            //    pass = allAttribute.All(a =>
            //     {
            //         var at = a.GetNamedValue("AspectType");
            //         if (at == null)
            //         {
            //             context.ReportDiagnostic(DiagnosticDefinitions.AAPG00001(source.TargetNode.GetLocation()));
            //             return false;
            //         }
            //         var att = (INamedTypeSymbol)at;
            //         if (!att.HasInterface("AutoAopProxyGenerator.IAspectHandler"))
            //         {
            //             context.ReportDiagnostic(DiagnosticDefinitions.AAPG00002(source.TargetNode.GetLocation()));
            //             return false;
            //         }
            //         return true;
            //     });


            //}
            //if (!pass)
            //{
            //    return;
            //}
            //#endregion

            //#region 获取所有的AspectHandler
            //var allHandlers = allInterfaces.SelectMany(x =>
            //{
            //    var handlers = x.GetAttributes(AspectHandler).Select(a =>
            //     {
            //         var at = a.GetNamedValue("AspectType");
            //         if (at == null)
            //         {
            //             context.ReportDiagnostic(DiagnosticDefinitions.AAPG00001(source.TargetNode.GetLocation()));
            //             return null;
            //         }
            //         var att = (INamedTypeSymbol)at;
            //         if (!att.HasInterface("AutoAopProxyGenerator.IAspectHandler"))
            //         {
            //             context.ReportDiagnostic(DiagnosticDefinitions.AAPG00002(source.TargetNode.GetLocation()));
            //             return null;
            //         }
            //         return att;
            //     });
            //    return handlers.Where(i => i != null).Cast<INamedTypeSymbol>();
            //}).Distinct(EqualityComparer<INamedTypeSymbol>.Default).ToArray();
            //#endregion

            if (!CheckAttributeEnable(context, source, allInterfaces, out var handlers))
            {
                return;
            }
            var file = CreateGeneratedProxyClassFile(targetSymbol, handlers);
            if (file != null)
            {
                //var ss = file.ToString();
                context.AddSource(file);
            }
        });
    }

    private static bool CheckAttributeEnable(SourceProductionContext context
        , GeneratorAttributeSyntaxContext source
        , INamedTypeSymbol[] all
        , out INamedTypeSymbol[] handlers)
    {
        List<INamedTypeSymbol> h = [];
        bool pass = true;
        foreach (var a in all)
        {
            // 获取接口和接口方法上标注的AddAspectHandlerAttribute
            var allAttribute = a.GetAttributes(AspectHandler).Concat(a.GetMethods().Where(m => m.MethodKind != MethodKind.Constructor).SelectMany(m => m.GetAttributes(AspectHandler)));
            foreach (var item in allAttribute)
            {
                var at = item.GetNamedValue("AspectType");
                if (at == null)
                {
                    context.ReportDiagnostic(DiagnosticDefinitions.AAPG00001(source.TargetNode.GetLocation()));
                    pass = false;
                    break;
                }
                var att = (INamedTypeSymbol)at;
                if (!att.HasInterface("AutoAopProxyGenerator.IAspectHandler"))
                {
                    context.ReportDiagnostic(DiagnosticDefinitions.AAPG00002(source.TargetNode.GetLocation()));
                    pass = false;
                    break;
                }
                h.Add(att);
            }
            if (!pass)
            {
                break;
            }
        }
        handlers = [.. h];
        return pass;
    }

    private static CodeFile? CreateGeneratedProxyClassFile(INamedTypeSymbol classSymbol, INamedTypeSymbol[] allHandlers)
    {
        // 代理字段和aspect handler 字段
        List<Node> members = [
            FieldBuilder.Default.MemberType(classSymbol.ToDisplayString()).FieldName("proxy")
            , .. allHandlers.Select(n =>FieldBuilder.Default.MemberType(n.ToDisplayString()).FieldName(n.MetadataName))
            ];
        // 构造函数
        List<Statement> ctorbody = [
            "this.proxy = proxy"
            , ..allHandlers.Select(n => $"this.{n.MetadataName} = {n.MetadataName}")
            ];
        var ctor = ConstructorBuilder.Default.MethodName($"{classSymbol.FormatClassName()}GeneratedProxy")
            .AddParameter([$"{classSymbol.ToDisplayString()} proxy", .. allHandlers.Select(n => $"{n.ToDisplayString()} {n.MetadataName}")]).AddBody([.. ctorbody]);
        members.Add(ctor);
        // 接口方法
        foreach (var iface in classSymbol.AllInterfaces)
        {
            members.AddRange(CreateProxyMethod(iface, classSymbol));
        }

        var proxyClass = ClassBuilder.Default
            .ClassName($"{classSymbol.FormatClassName()}GeneratedProxy")
            .AddMembers([.. members])
            .Interface([.. classSymbol.Interfaces.Select(i => i.ToDisplayString())])
            .AddGeneratedCodeAttribute(typeof(AutoAopProxyClassGenerator));

        return CodeFile.New($"{classSymbol.FormatFileName()}GeneratedProxyClass.g.cs")
            .AddUsings("using AutoAopProxyGenerator;")
            .AddMembers(NamespaceBuilder.Default.Namespace(classSymbol.ContainingNamespace.ToDisplayString()).AddMembers(proxyClass));
    }

    private static IEnumerable<MethodBuilder> CreateProxyMethod(INamedTypeSymbol iface, INamedTypeSymbol classSymbol)
    {
        var handlers = iface.GetAttributes(AspectHandler).Select(a => a.GetNamedValue("AspectType")).OfType<INamedTypeSymbol>().ToArray();

        foreach (var m in iface.GetMethods())
        {
            MethodBuilder methodBuilder;
            var methodHandlers = m.GetAttributes(AspectHandler).Select(a => a.GetNamedValue("AspectType")).OfType<INamedTypeSymbol>();
            INamedTypeSymbol[] usedHandlers = [.. handlers, .. methodHandlers];
            if (usedHandlers.Length == 0 || m.HasAttribute(IgnoreAspect)
                //|| (!iface.HasAttribute(AspectHandler) && !m.HasAttribute(AspectHandler))
                )
            {
                methodBuilder = CreateDirectInvokeMethod(m);
            }
            else
            {

                methodBuilder = CreateProxyMethod(m, usedHandlers);
            }

            yield return methodBuilder;
        }

        MethodBuilder CreateDirectInvokeMethod(IMethodSymbol m)
        {
            var builder = MethodBuilder.Default
                .MethodName(m.Name)
                .Generic([.. m.GetTypeParameters()])
                .AddParameter([.. m.Parameters.Select(p => $"{p.Type.ToDisplayString()} {p.Name}")])
                .ReturnType(m.ReturnType.ToDisplayString())
                .AddGeneratedCodeAttribute(typeof(AutoAopProxyClassGenerator));
            builder = builder
                .Lambda($"proxy.{builder.ConstructedMethodName}({string.Join(", ", m.Parameters.Select(p => p.Name))})");

            return builder;
        }

        MethodBuilder CreateProxyMethod(IMethodSymbol m, INamedTypeSymbol[] methodHandlers)
        {
            var method = m.IsGenericMethod ? m.ConstructedFrom : m;
            var returnType = method.ReturnType.GetGenericTypes().FirstOrDefault() ?? method.ReturnType;
            var isAsync = method.IsAsync || method.ReturnType.ToDisplayString().StartsWith("System.Threading.Tasks.Task");
            var builder = MethodBuilder.Default
                 .MethodName(method.Name)
                 .Async(isAsync)
                 .Generic([.. method.GetTypeParameters()])
                 .AddParameter([.. method.Parameters.Select(p => $"{p.Type.ToDisplayString()} {p.Name}")])
                 .ReturnType(method.ReturnType.ToDisplayString())
                 .AddGeneratedCodeAttribute(typeof(AutoAopProxyClassGenerator));
            List<Statement> statements = [];
            var hasReturn = !method.ReturnsVoid && returnType.ToDisplayString() != "System.Threading.Tasks.Task";
            //if (hasReturn)
            //{
            //    statements.Add($"{returnType.ToDisplayString()} returnValue = default;");
            //}
            var done = LocalFunction.Default
                .MethodName("Done")
                .AddParameters("ProxyContext _ctx_gen")
                .Async(isAsync)
                .Return("System.Threading.Tasks.Task")
                .AddBody([.. CreateLocalFunctionBody(method, builder.ConstructedMethodName, isAsync, hasReturn)]);

            statements.Add(done);
            statements.Add("var _builder_gen = AsyncPipelineBuilder<ProxyContext>.Create(Done)");
            foreach (var handler in methodHandlers)
            {
                statements.Add($"_builder_gen.Use(this.{handler.MetadataName}.Invoke)");
            }
            statements.Add("var _job_gen = _builder_gen.Build()");
            var ptypes = method.Parameters.Length > 0 ? $"[{string.Join(", ", method.Parameters.Select(SelectParameterType))}]" : "Type.EmptyTypes";
            statements.Add($"var _context_gen = ContextHelper<{iface.ToDisplayString()}, {classSymbol.ToDisplayString()}>.GetOrCreate(nameof({method.Name}), {ptypes})");
            if (hasReturn)
            {
                statements.Add("_context_gen.HasReturnValue = true");
            }
            statements.Add($"_context_gen.Parameters = new object?[] {{{string.Join(", ", method.Parameters.Select(p => p.Name))}}};");
            if (isAsync)
            {
                statements.Add("await _job_gen.Invoke(_context_gen)");
            }
            else
            {
                statements.Add("_job_gen.Invoke(_context_gen).GetAwaiter().GetResult()");
            }
            if (hasReturn)
                statements.Add($"return ({returnType.ToDisplayString()})_context_gen.ReturnValue");

            builder.AddBody([.. statements]);

            return builder;
        }
    }
    // TODO修改返回值为string
    private static object SelectParameterType(IParameterSymbol p)
    {
        if (p.Type.NullableAnnotation == NullableAnnotation.Annotated)
        {
            return $"typeof({p.Type.WithNullableAnnotation(NullableAnnotation.NotAnnotated).ToDisplayString()})";
        }
        return $"typeof({p.Type.ToDisplayString()})";
    }

    private static IEnumerable<Statement> CreateLocalFunctionBody(IMethodSymbol method, string proxyName, bool isAsync, bool hasReturn)
    {
        if (isAsync)
        {
            if (hasReturn)
            {
                yield return $"var _val_gen = await proxy.{proxyName}({string.Join(", ", method.Parameters.Select(p => p.Name))})";
                yield return "_ctx_gen.Executed = true";
                yield return "_ctx_gen.SetReturnValue(_val_gen)";
            }
            else
            {
                yield return $"await proxy.{proxyName}({string.Join(", ", method.Parameters.Select(p => p.Name))})";
                yield return "_ctx_gen.Executed = true";
            }
        }
        else
        {
            if (hasReturn)
            {
                yield return $"var _val_gen = proxy.{proxyName}({string.Join(", ", method.Parameters.Select(p => p.Name))})";
                yield return "_ctx_gen.Executed = true";
                yield return "_ctx_gen.SetReturnValue(_val_gen)";
                yield return "return global::System.Threading.Tasks.Task.CompletedTask";
            }
            else
            {
                yield return $"proxy.{proxyName}({string.Join(", ", method.Parameters.Select(p => p.Name))})";
                yield return "_ctx_gen.Executed = true";
                yield return "return global::System.Threading.Tasks.Task.CompletedTask";
            }
        }
    }
}
