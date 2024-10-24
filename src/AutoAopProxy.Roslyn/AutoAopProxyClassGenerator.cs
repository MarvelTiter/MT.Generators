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

            if (!CheckAttributeEnable(context, source, allInterfaces, out var handlers))
            {
                return;
            }
            var file = CreateGeneratedProxyClassFile(targetSymbol, handlers);
            if (file != null)
            {
#if DEBUG
                var ss = file.ToString();
#endif
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
        handlers = [.. h.Distinct(EqualityComparer<INamedTypeSymbol>.Default)];
        return pass;
    }

    private static CodeFile? CreateGeneratedProxyClassFile(INamedTypeSymbol classSymbol, INamedTypeSymbol[] allHandlers)
    {
        var np = NamespaceBuilder.Default.Namespace(classSymbol.ContainingNamespace.ToDisplayString()).FileScoped();
        var proxyClass = ClassBuilder.Default;
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
        List<INamedTypeSymbol> outterHandlers = [];
        foreach (var iface in classSymbol.Interfaces)
        {
            //members.AddRange();
            CreateInterfaceProxyMethod(classSymbol, iface, members, outterHandlers);
        }

        proxyClass.ClassName($"{classSymbol.FormatClassName()}GeneratedProxy")
            .AddMembers([.. members])
            .Generic([.. classSymbol.GetTypeParameters()])
            .Interface([.. classSymbol.Interfaces.Select(i => i.ToDisplayString())])
            .AddGeneratedCodeAttribute(typeof(AutoAopProxyClassGenerator));

        return CodeFile.New($"{classSymbol.FormatFileName()}GeneratedProxyClass.g.cs")
            .AddUsings("using AutoAopProxyGenerator;")
            .AddMembers(np.AddMembers(proxyClass));
    }

    private static void CreateInterfaceProxyMethod(INamedTypeSymbol classSymbol, INamedTypeSymbol iface, List<Node> parentMembers, List<INamedTypeSymbol> outterHandlers)
    {
        // 自身的切面处理器
        var infos = iface.GetAttributes(AspectHandler).Select(GetAttrInfo).ToArray();
        var handlers = infos.Select(i => i.handler);
        INamedTypeSymbol[] interfaceHandles = [.. handlers, .. outterHandlers];
        interfaceHandles = interfaceHandles.Distinct(EqualityComparer<INamedTypeSymbol>.Default).ToArray();
        var methods = iface.GetMethods();
        foreach (var m in iface.GetMethods())
        {
            MethodBuilder methodBuilder;
            //var methodHandlers = m.GetAttributes(AspectHandler).Select(a => a.GetNamedValue("AspectType")).OfType<INamedTypeSymbol>();
            INamedTypeSymbol[] usedHandlers = GetMethodEnabledHandlers(m, interfaceHandles).Distinct(EqualityComparer<INamedTypeSymbol>.Default).ToArray();
            if (usedHandlers.Length == 0
                //|| m.HasAttribute(IgnoreAspect)
                //|| (!iface.HasAttribute(AspectHandler) && !m.HasAttribute(AspectHandler))
                )
            {
                methodBuilder = CreateDirectInvokeMethod(m);
            }
            else
            {
                methodBuilder = CreateProxyMethod(classSymbol, iface, m, usedHandlers);
            }
            //yield return methodBuilder;
            parentMembers.Add(methodBuilder);
        }

        if (iface.Interfaces.Length > 0)
        {
            foreach (var (selfOnly, handler) in infos)
            {
                if (!selfOnly)
                {
                    outterHandlers.Add(handler);
                }
            }
            foreach (var item in iface.Interfaces)
            {
                CreateInterfaceProxyMethod(classSymbol, item, parentMembers, outterHandlers);
            }
        }

        static INamedTypeSymbol[] GetMethodEnabledHandlers(IMethodSymbol methodSymbol, INamedTypeSymbol[] interfaceHandles)
        {
            var methodHandlers = methodSymbol.GetAttributes(AspectHandler).Select(a => a.GetNamedValue("AspectType")).OfType<INamedTypeSymbol>();
            if (!methodSymbol.GetAttribute(IgnoreAspect, out var ignoreInfo))
            {
                // 没有标注IgnoreAspect，直接返回所有可用的
                return [.. interfaceHandles, .. methodHandlers];
            }
            _ = ignoreInfo!.GetConstructorValues(0, out var values);
            if (values.Length == 0)
            {
                // 标注了IgnoreAspect，但是没有指定忽略什么，则忽略全部，除了在自身标注的
                return [.. methodHandlers];
            }
            // 指定忽略的类型
            INamedTypeSymbol[] ignoreTypes = [.. values.OfType<INamedTypeSymbol>()];
            List<INamedTypeSymbol> all = [.. interfaceHandles];
            foreach (var item in ignoreTypes)
            {
                all.Remove(item);
            }
            return [.. all, .. methodHandlers];
        }

        static MethodBuilder CreateDirectInvokeMethod(IMethodSymbol m)
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

        static MethodBuilder CreateProxyMethod(INamedTypeSymbol classSymbol, INamedTypeSymbol iface, IMethodSymbol methodSymbol, INamedTypeSymbol[] methodHandlers)
        {
            var method = methodSymbol.IsGenericMethod ? methodSymbol.ConstructedFrom : methodSymbol;
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
                statements.Add($"return _context_gen.Return<{returnType.ToDisplayString()}>()");

            builder.AddBody([.. statements]);

            return builder;
        }

        static (bool selfOnly, INamedTypeSymbol handler) GetAttrInfo(AttributeData attributeData)
        {
            var handler = attributeData.GetNamedValue("AspectType") as INamedTypeSymbol;
            var selfonly = attributeData.GetNamedValue("SelfOnly");
            var b = selfonly is bool self && self;
            return (b, handler!);
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
                yield return "_ctx_gen.SetReturnValue(_val_gen, ExecuteStatus.Executed)";
            }
            else
            {
                yield return $"await proxy.{proxyName}({string.Join(", ", method.Parameters.Select(p => p.Name))})";
                yield return "_ctx_gen.SetStatus(ExecuteStatus.Executed)";
            }
        }
        else
        {
            if (hasReturn)
            {
                yield return $"var _val_gen = proxy.{proxyName}({string.Join(", ", method.Parameters.Select(p => p.Name))})";
                yield return "_ctx_gen.SetReturnValue(_val_gen, ExecuteStatus.Executed)";
                yield return "return global::System.Threading.Tasks.Task.CompletedTask";
            }
            else
            {
                yield return $"proxy.{proxyName}({string.Join(", ", method.Parameters.Select(p => p.Name))})";
                yield return "_ctx_gen.SetStatus(ExecuteStatus.Executed)";
                yield return "return global::System.Threading.Tasks.Task.CompletedTask";
            }
        }
    }
}
