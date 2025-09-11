using Generators.Shared;
using Generators.Shared.Builder;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Diagnostics.SymbolStore;
using System.Linq;
using System.Text;
using static AutoAopProxyGenerator.GeneratorHelper;
namespace AutoAopProxyGenerator;

[Generator(LanguageNames.CSharp)]
public class AutoAopProxyClassGenerator : IIncrementalGenerator
{

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var source = context.SyntaxProvider.ForAttributeWithMetadataName(
            Aspectable
            , static (node, _) => node is ClassDeclarationSyntax
            , static (ctx, _) => CollectContextInfo(ctx)).Collect();

        context.RegisterSourceOutput(source, static (context, source) =>
        {
            foreach (var item in source)
            {
                if (item.Diagnostic is not null)
                {
                    context.ReportDiagnostic(item.Diagnostic);
                    continue;
                }
                var file = CreateCodeFile(item);
#if DEBUG
                var ss = file.ToString();
#endif
                context.AddSource(file);
            }
        });


        //        var source = context.SyntaxProvider.ForAttributeWithMetadataName(
        //            Aspectable
        //            , static (node, _) => node is ClassDeclarationSyntax
        //            , static (ctx, _) => (ctx));
        //        context.RegisterSourceOutput(source, static (context, source) =>
        //        {
        //            var targetSymbol = (INamedTypeSymbol)source.TargetSymbol;
        //            //INamedTypeSymbol[] needToCheckSymbol = [targetSymbol, .. targetSymbol.AllInterfaces];

        //            var allInterfaces = targetSymbol.AllInterfaces.Where(a =>
        //            {
        //                // 接口上标注了AddAspectHandlerAttribute，或者接口中有方法标注了AddAspectHandlerAttribute
        //                return a.HasAttribute(AspectHandler) || a.GetMethods().Where(m => m.MethodKind != MethodKind.Constructor).Any(m => m.HasAttribute(AspectHandler));
        //            }).ToArray();

        //            if (!CheckAttributeEnable(context, source, allInterfaces, out var handlers))
        //            {
        //                return;
        //            }
        //            var file = CreateGeneratedProxyClassFile(targetSymbol, handlers);
        //            if (file != null)
        //            {
        //#if DEBUG
        //                var ss = file.ToString();
        //#endif
        //                context.AddSource(file);
        //            }
        //        });
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

        var enableInterfaces = GetInterfacesIncludeBaseType(classSymbol);
        // 接口方法
        //List<INamedTypeSymbol> outterHandlers = [];
        List<MethodContext> methodContexts = [];
        foreach (var iface in enableInterfaces)
        {
            //members.AddRange();
            //CreateInterfaceProxyMethod(classSymbol, iface, members, outterHandlers);
            var contexts = CollectInterfacesMethod(iface, []);

            // 首先比较contexts内部的元素
            for (int i = 0; i < contexts.Count; i++)
            {
                for (int j = i + 1; j < contexts.Count; j++)
                {
                    if (contexts[i].Symbol.AreMethodsSignatureEqual(contexts[j].Symbol))
                    {
                        contexts[i].IsExplicit = true;
                        contexts[j].IsExplicit = true;
                    }
                }
            }

            foreach (var cur in contexts)
            {
                foreach (var pre in methodContexts)
                {
                    if (cur.Symbol.AreMethodsSignatureEqual(pre.Symbol))
                    {
                        cur.IsExplicit = true;
                        pre.IsExplicit = true;
                    }
                }
            }
            methodContexts.AddRange(contexts);
        }

        foreach (var item in methodContexts)
        {
            MethodBuilder methodBuilder;
            if (item.Handlers.Length > 0)
            {
                methodBuilder = CreateProxyMethod(classSymbol, item);
            }
            else
            {
                methodBuilder = CreateDirectInvokeMethod(item);
            }
            members.Add(methodBuilder);
        }

        proxyClass.ClassName($"{classSymbol.FormatClassName()}GeneratedProxy")
            .AddMembers([.. members])
            .Generic([.. classSymbol.GetTypeParameters()])
            .Interface([.. enableInterfaces.Select(i => i.ToDisplayString())])
            .AddGeneratedCodeAttribute(typeof(AutoAopProxyClassGenerator));

        return CodeFile.New($"{classSymbol.FormatFileName()}GeneratedProxyClass.g.cs")
            .AddUsings("using AutoAopProxyGenerator;")
            .AddMembers(np.AddMembers(proxyClass));
    }
    private static List<MethodContext> CollectInterfacesMethod(INamedTypeSymbol iSymbol, INamedTypeSymbol[] parentHandlers)
    {
        var currentHandlers = iSymbol.GetAttributes(AspectHandler).Select(CollectAttributeInfo);
        List<MethodContext> result = [];

        foreach (var m in iSymbol.GetMethods())
        {
            var all = GetMethodEnableHandlers(m, parentHandlers).Distinct(EqualityComparer<INamedTypeSymbol>.Default);
            var ctx = new MethodContext(m, iSymbol)
            {
                Handlers = [.. all],
            };
            result.Add(ctx);
        }

        // 检查继承的接口
        var iparents = iSymbol.GetInterfaces().ToArray();
        if (iparents.Length > 0)
        {
            parentHandlers = [.. parentHandlers, .. currentHandlers.Where(h => !h.IsSelf).Select(h => h.Handler)];
            foreach (var item in iparents)
            {
                var children = CollectInterfacesMethod(item, parentHandlers);
                result.AddRange(children);
            }
        }
        return result;

        HandlerInfo CollectAttributeInfo(AttributeData data)
        {
            var handler = data.GetNamedValue("AspectType") as INamedTypeSymbol;
            var selfonly = data.GetNamedValue("SelfOnly");
            var b = selfonly is bool self && self;
            return new HandlerInfo(b, iSymbol, handler!);
        }

        static INamedTypeSymbol[] GetMethodEnableHandlers(IMethodSymbol methodSymbol, INamedTypeSymbol[] parentHandlers)
        {
            var methodHandlers = methodSymbol.GetAttributes(AspectHandler).Select(a => a.GetNamedValue("AspectType")).OfType<INamedTypeSymbol>();
            if (!methodSymbol.GetAttribute(IgnoreAspect, out var ignoreInfo))
            {
                // 没有标注IgnoreAspect，直接返回所有可用的
                return [.. parentHandlers, .. methodHandlers];
            }
            _ = ignoreInfo!.GetConstructorValues(0, out var values);
            if (values.Length == 0)
            {
                // 标注了IgnoreAspect，但是没有指定忽略什么，则忽略全部，除了在自身标注的
                return [.. methodHandlers];
            }
            // 指定忽略的类型
            INamedTypeSymbol[] ignoreTypes = [.. values.OfType<INamedTypeSymbol>()];
            List<INamedTypeSymbol> all = [.. parentHandlers];
            foreach (var item in ignoreTypes)
            {
                all.Remove(item);
            }
            return [.. all, .. methodHandlers];
        }
    }

    private static object SelectParameterType(IParameterSymbol p)
    {
        if (p.Type.NullableAnnotation == NullableAnnotation.Annotated)
        {
            return $"typeof({p.Type.WithNullableAnnotation(NullableAnnotation.NotAnnotated).ToDisplayString()})";
        }
        return $"typeof({p.Type.ToDisplayString()})";
    }

    private static IEnumerable<Statement> CreateLocalFunctionBody(INamedTypeSymbol iSymbol, IMethodSymbol method, string proxyName, bool isExplicit, bool isAsync, bool hasReturn)
    {
        var obj = isExplicit ? $"(({iSymbol.ToDisplayString()})proxy)" : "proxy";
        if (isAsync)
        {
            if (hasReturn)
            {
                yield return $"var _val_gen = await {obj}.{proxyName}({string.Join(", ", method.Parameters.Select(p => p.Name))})";
                yield return "_ctx_gen.SetReturnValue(_val_gen, ExecuteStatus.Executed)";
            }
            else
            {
                yield return $"await {obj}.{proxyName}({string.Join(", ", method.Parameters.Select(p => p.Name))})";
                yield return "_ctx_gen.SetStatus(ExecuteStatus.Executed)";
            }
        }
        else
        {
            if (hasReturn)
            {
                yield return $"var _val_gen = {obj}.{proxyName}({string.Join(", ", method.Parameters.Select(p => p.Name))})";
                yield return "_ctx_gen.SetReturnValue(_val_gen, ExecuteStatus.Executed)";
                yield return "return global::System.Threading.Tasks.Task.CompletedTask";
            }
            else
            {
                yield return $"{obj}.{proxyName}({string.Join(", ", method.Parameters.Select(p => p.Name))})";
                yield return "_ctx_gen.SetStatus(ExecuteStatus.Executed)";
                yield return "return global::System.Threading.Tasks.Task.CompletedTask";
            }
        }
    }

    private static List<INamedTypeSymbol> GetInterfacesIncludeBaseType(INamedTypeSymbol classSymbol)
    {
        var all = new List<INamedTypeSymbol>();
        all.AddRange(classSymbol.Interfaces);
        if (classSymbol.BaseType is not null)
        {
            var parents = GetInterfacesIncludeBaseType(classSymbol.BaseType);
            all.AddRange(parents);
        }
        return [.. all.Distinct(EqualityComparer<INamedTypeSymbol>.Default)];
    }

    private static MethodBuilder CreateDirectInvokeMethod(MethodContext context)
    {
        var symbol = context.Symbol;
        var builder = MethodBuilder.Default
            .MethodName(symbol.Name)
            .Generic([.. symbol.GetTypeParameters()])
            .AddParameter([.. symbol.Parameters.Select(p => $"{p.Type.ToDisplayString()} {p.Name}")])
            .ReturnType(symbol.ReturnType.ToDisplayString())
            .AddGeneratedCodeAttribute(typeof(AutoAopProxyClassGenerator));
        if (context.IsExplicit)
        {
            builder.ExplicitFor(context.DeclaredType.ToDisplayString());
            builder.Lambda($"(({context.DeclaredType.ToDisplayString()})proxy).{builder.ConstructedMethodName}({string.Join(", ", symbol.Parameters.Select(p => p.Name))})");
        }
        else
        {
            builder.Lambda($"proxy.{builder.ConstructedMethodName}({string.Join(", ", symbol.Parameters.Select(p => p.Name))})");
        }
        return builder;
    }

    // INamedTypeSymbol cSymbol, INamedTypeSymbol iSymbol, IMethodSymbol methodSymbol, INamedTypeSymbol[] methodHandlers
    private static MethodBuilder CreateProxyMethod(INamedTypeSymbol cSymbol, MethodContext context)
    {
        var methodSymbol = context.Symbol;
        var method = methodSymbol.IsGenericMethod ? methodSymbol.ConstructedFrom : methodSymbol;
        var methodHandlers = context.Handlers;
        var iSymbol = context.DeclaredType;
        var (IsTask, HasReturn, ReturnType) = method.GetReturnTypeInfo();
        //var isAsync = method.IsAsync || method.ReturnType.ToDisplayString().StartsWith("System.Threading.Tasks.Task");
        var builder = MethodBuilder.Default
             .MethodName(method.Name)
             .Async(IsTask)
             .Generic([.. method.GetTypeParameters()])
             .AddParameter([.. method.Parameters.Select(p => $"{p.Type.ToDisplayString()} {p.Name}")])
             .ReturnType(method.ReturnType.ToDisplayString())
             .AddGeneratedCodeAttribute(typeof(AutoAopProxyClassGenerator));
        List<Statement> statements = [];
        //var hasReturn = !method.ReturnsVoid && ReturnType.ToDisplayString() != "System.Threading.Tasks.Task";
        //if (hasReturn)
        //{
        //    statements.Add($"{returnType.ToDisplayString()} returnValue = default;");
        //}
        var done = LocalFunction.Default
            .MethodName("Done")
            .AddParameters("ProxyContext _ctx_gen")
            .Async(IsTask)
            .Return("System.Threading.Tasks.Task")
            .AddBody([.. CreateLocalFunctionBody(iSymbol, method, builder.ConstructedMethodName, context.IsExplicit, IsTask, HasReturn)]);

        statements.Add(done);
        statements.Add("var _builder_gen = AsyncPipelineBuilder<ProxyContext>.Create(Done)");
        foreach (var handler in methodHandlers)
        {
            statements.Add($"_builder_gen.Use(this.{handler.MetadataName}.Invoke)");
        }
        statements.Add("var _job_gen = _builder_gen.Build()");
        var ptypes = method.Parameters.Length > 0 ? $"[{string.Join(", ", method.Parameters.Select(SelectParameterType))}]" : "Type.EmptyTypes";
        statements.Add($"var _context_gen = ContextHelper<{iSymbol.ToDisplayString()}, {cSymbol.ToDisplayString()}>.GetOrCreate(nameof({method.Name}), {ptypes})");

        statements.Add($"_context_gen.Parameters = new object?[] {{{string.Join(", ", method.Parameters.Select(p => p.Name))}}};");
        if (IsTask)
        {
            statements.Add("await _job_gen.Invoke(_context_gen)");
        }
        else
        {
            statements.Add("_job_gen.Invoke(_context_gen).GetAwaiter().GetResult()");
        }
        if (HasReturn)
            statements.Add($"return _context_gen.Return<{ReturnType.ToDisplayString()}>()");

        builder.AddBody([.. statements]);

        if (context.IsExplicit)
        {
            builder.ExplicitFor(iSymbol.ToDisplayString());
        }

        return builder;
    }
    //private static void CreateInterfaceProxyMethod(INamedTypeSymbol classSymbol, INamedTypeSymbol iface, List<Node> parentMembers, List<INamedTypeSymbol> outterHandlers)
    //{
    //    // 自身的切面处理器
    //    var infos = iface.GetAttributes(AspectHandler).Select(GetAttrInfo).ToArray();
    //    var handlers = infos.Select(i => i.handler);
    //    INamedTypeSymbol[] interfaceHandles = [.. handlers, .. outterHandlers];
    //    interfaceHandles = interfaceHandles.Distinct(EqualityComparer<INamedTypeSymbol>.Default).ToArray();
    //    var methods = iface.GetMethods();
    //    foreach (var m in iface.GetMethods())
    //    {
    //        MethodBuilder methodBuilder;
    //        //var methodHandlers = m.GetAttributes(AspectHandler).Select(a => a.GetNamedValue("AspectType")).OfType<INamedTypeSymbol>();
    //        INamedTypeSymbol[] usedHandlers = GetMethodEnabledHandlers(m, interfaceHandles).Distinct(EqualityComparer<INamedTypeSymbol>.Default).ToArray();
    //        if (usedHandlers.Length == 0
    //            //|| m.HasAttribute(IgnoreAspect)
    //            //|| (!iface.HasAttribute(AspectHandler) && !m.HasAttribute(AspectHandler))
    //            )
    //        {
    //            methodBuilder = CreateDirectInvokeMethod(m);
    //        }
    //        else
    //        {
    //            methodBuilder = CreateProxyMethod(classSymbol, iface, m, usedHandlers);
    //        }
    //        //yield return methodBuilder;
    //        parentMembers.Add(methodBuilder);
    //    }
    //    var ifaces = iface.GetInterfaces().ToArray();
    //    if (ifaces.Length > 0)
    //    {
    //        foreach (var (selfOnly, handler) in infos)
    //        {
    //            if (!selfOnly)
    //            {
    //                outterHandlers.Add(handler);
    //            }
    //        }
    //        foreach (var item in ifaces)
    //        {
    //            CreateInterfaceProxyMethod(classSymbol, item, parentMembers, outterHandlers);
    //        }
    //    }

    //    static INamedTypeSymbol[] GetMethodEnabledHandlers(IMethodSymbol methodSymbol, INamedTypeSymbol[] interfaceHandles)
    //    {
    //        var methodHandlers = methodSymbol.GetAttributes(AspectHandler).Select(a => a.GetNamedValue("AspectType")).OfType<INamedTypeSymbol>();
    //        if (!methodSymbol.GetAttribute(IgnoreAspect, out var ignoreInfo))
    //        {
    //            // 没有标注IgnoreAspect，直接返回所有可用的
    //            return [.. interfaceHandles, .. methodHandlers];
    //        }
    //        _ = ignoreInfo!.GetConstructorValues(0, out var values);
    //        if (values.Length == 0)
    //        {
    //            // 标注了IgnoreAspect，但是没有指定忽略什么，则忽略全部，除了在自身标注的
    //            return [.. methodHandlers];
    //        }
    //        // 指定忽略的类型
    //        INamedTypeSymbol[] ignoreTypes = [.. values.OfType<INamedTypeSymbol>()];
    //        List<INamedTypeSymbol> all = [.. interfaceHandles];
    //        foreach (var item in ignoreTypes)
    //        {
    //            all.Remove(item);
    //        }
    //        return [.. all, .. methodHandlers];
    //    }

    //    static MethodBuilder CreateDirectInvokeMethod(IMethodSymbol m)
    //    {
    //        var builder = MethodBuilder.Default
    //            .MethodName(m.Name)
    //            .Generic([.. m.GetTypeParameters()])
    //            .AddParameter([.. m.Parameters.Select(p => $"{p.Type.ToDisplayString()} {p.Name}")])
    //            .ReturnType(m.ReturnType.ToDisplayString())
    //            .AddGeneratedCodeAttribute(typeof(AutoAopProxyClassGenerator));
    //        builder = builder
    //            .Lambda($"proxy.{builder.ConstructedMethodName}({string.Join(", ", m.Parameters.Select(p => p.Name))})");

    //        return builder;
    //    }

    //    static MethodBuilder CreateProxyMethod(INamedTypeSymbol classSymbol, INamedTypeSymbol iface, IMethodSymbol methodSymbol, INamedTypeSymbol[] methodHandlers)
    //    {
    //        var method = methodSymbol.IsGenericMethod ? methodSymbol.ConstructedFrom : methodSymbol;
    //        var returnType = method.ReturnType.GetGenericTypes().FirstOrDefault() ?? method.ReturnType;
    //        var isAsync = method.IsAsync || method.ReturnType.ToDisplayString().StartsWith("System.Threading.Tasks.Task");
    //        var builder = MethodBuilder.Default
    //             .MethodName(method.Name)
    //             .Async(isAsync)
    //             .Generic([.. method.GetTypeParameters()])
    //             .AddParameter([.. method.Parameters.Select(p => $"{p.Type.ToDisplayString()} {p.Name}")])
    //             .ReturnType(method.ReturnType.ToDisplayString())
    //             .AddGeneratedCodeAttribute(typeof(AutoAopProxyClassGenerator));
    //        List<Statement> statements = [];
    //        var hasReturn = !method.ReturnsVoid && returnType.ToDisplayString() != "System.Threading.Tasks.Task";
    //        //if (hasReturn)
    //        //{
    //        //    statements.Add($"{returnType.ToDisplayString()} returnValue = default;");
    //        //}
    //        var done = LocalFunction.Default
    //            .MethodName("Done")
    //            .AddParameters("ProxyContext _ctx_gen")
    //            .Async(isAsync)
    //            .Return("System.Threading.Tasks.Task")
    //            .AddBody([.. CreateLocalFunctionBody(method, builder.ConstructedMethodName, isAsync, hasReturn)]);

    //        statements.Add(done);
    //        statements.Add("var _builder_gen = AsyncPipelineBuilder<ProxyContext>.Create(Done)");
    //        foreach (var handler in methodHandlers)
    //        {
    //            statements.Add($"_builder_gen.Use(this.{handler.MetadataName}.Invoke)");
    //        }
    //        statements.Add("var _job_gen = _builder_gen.Build()");
    //        var ptypes = method.Parameters.Length > 0 ? $"[{string.Join(", ", method.Parameters.Select(SelectParameterType))}]" : "Type.EmptyTypes";
    //        statements.Add($"var _context_gen = ContextHelper<{iface.ToDisplayString()}, {classSymbol.ToDisplayString()}>.GetOrCreate(nameof({method.Name}), {ptypes})");

    //        statements.Add($"_context_gen.Parameters = new object?[] {{{string.Join(", ", method.Parameters.Select(p => p.Name))}}};");
    //        if (isAsync)
    //        {
    //            statements.Add("await _job_gen.Invoke(_context_gen)");
    //        }
    //        else
    //        {
    //            statements.Add("_job_gen.Invoke(_context_gen).GetAwaiter().GetResult()");
    //        }
    //        if (hasReturn)
    //            statements.Add($"return _context_gen.Return<{returnType.ToDisplayString()}>()");

    //        builder.AddBody([.. statements]);

    //        return builder;
    //    }

    //    static (bool selfOnly, INamedTypeSymbol handler) GetAttrInfo(AttributeData attributeData)
    //    {
    //        var handler = attributeData.GetNamedValue("AspectType") as INamedTypeSymbol;
    //        var selfonly = attributeData.GetNamedValue("SelfOnly");
    //        var b = selfonly is bool self && self;
    //        return (b, handler!);
    //    }
    //}
    // TODO修改返回值为string
}
