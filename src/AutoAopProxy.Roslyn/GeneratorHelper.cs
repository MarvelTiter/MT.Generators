using AutoAopProxyGenerator.Models;
using Generators.Shared;
using Generators.Shared.Builder;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AutoAopProxyGenerator;

internal static class GeneratorHelper
{
    public const string Aspectable = "AutoAopProxyGenerator.GenAspectProxyAttribute";
    public const string AspectHandler = "AutoAopProxyGenerator.AddAspectHandlerAttribute";
    public const string IgnoreAspect = "AutoAopProxyGenerator.IgnoreAspectAttribute";
    public static GenerateContext CollectContextInfo(GeneratorAttributeSyntaxContext context)
    {
        var targetSymbol = (INamedTypeSymbol)context.TargetSymbol;
        targetSymbol.GetAttribute(Aspectable, out var attributeData);
        var allHandlerInterfaces = targetSymbol.AllInterfaces.Where(a =>
        {
            // 接口上标注了AddAspectHandlerAttribute，或者接口中有方法标注了AddAspectHandlerAttribute
            return a.HasAttribute(AspectHandler) || a.GetMethods().Where(m => m.MethodKind != MethodKind.Constructor).Any(m => m.HasAttribute(AspectHandler));
        }).ToArray();
        var error = CheckAttributeEnable();
        if (error is not null)
        {
            return new GenerateContext(targetSymbol, attributeData!)
            {
                Diagnostic = error
            };
        }
        var allHandlers = CollectClassHandlerInfo(allHandlerInterfaces);
        var proxyInterfaces = GetInterfacesIncludeBaseType(targetSymbol);
        List<AspectMemberContext> methodContexts = [];
        foreach (INamedTypeSymbol item in proxyInterfaces)
        {
            ScanSymbolMethodsRecursive(item, e =>
            {
                CheckExplicit(methodContexts, e);
                foreach (var mh in e.MethodHandlers)
                {
                    if (!allHandlers.Any(h => EqualityComparer<INamedTypeSymbol>.Default.Equals(h.Handler, mh)))
                    {
                        allHandlers.Add(new HandlerInfo(true, e.DeclaredType, mh));
                    }
                }
            });
        }

        //if (targetSymbol.BaseType is not null && attributeData.GetNamedValue<bool>("InheritBaseType", out var b) && b)
        //{
        //    foreach (var m in targetSymbol.GetMembers())
        //    {
        //        if (m is IMethodSymbol method && method.MethodKind == MethodKind.Ordinary)
        //        {
        //            if (!method.IsOverride)
        //                continue;
        //        }
        //        else if (m is IPropertySymbol prop)
        //        {
        //            if (!prop.IsOverride)
        //                continue;
        //        }
        //        else
        //        {
        //            continue;
        //        }
        //        var ctx = new AspectMemberContext(m, targetSymbol.BaseType)
        //        {
        //            IsOverride = true,
        //            Type = m is IMethodSymbol ? HandlerType.Method : HandlerType.Property
        //        };
        //        methodContexts.Add(ctx);
        //    }
        //}

        return new(targetSymbol, attributeData!)
        {
            AllHandlers = allHandlers,
            AllMethods = methodContexts,
            ProxyInterfaces = [.. proxyInterfaces]
        };

        Diagnostic? CheckAttributeEnable()
        {
            foreach (var a in allHandlerInterfaces)
            {
                // 获取接口和接口方法上标注的AddAspectHandlerAttribute
                var allAttribute = a.GetAttributes(AspectHandler).Concat(a.GetMethods().Where(m => m.MethodKind != MethodKind.Constructor).SelectMany(m => m.GetAttributes(AspectHandler)));
                foreach (var item in allAttribute)
                {
                    var at = item.GetNamedValue("AspectType");
                    if (at == null)
                    {
                        return DiagnosticDefinitions.AAPG00001(context.TargetNode.GetLocation());
                    }
                    var att = (INamedTypeSymbol)at;
                    if (!att.HasInterface("AutoAopProxyGenerator.IAspectHandler"))
                    {
                        return DiagnosticDefinitions.AAPG00002(context.TargetNode.GetLocation());
                    }
                }
            }
            return null;
        }

        static List<HandlerInfo> CollectClassHandlerInfo(INamedTypeSymbol[] typeSymbols)
        {
            List<HandlerInfo> infos = [];
            foreach (var item in typeSymbols)
            {
                var l1 = item.GetAttributes(AspectHandler).Select(a =>
                  {
                      var handler = a.GetNamedValue("AspectType") as INamedTypeSymbol;
                      var selfonly = a.GetNamedValue("SelfOnly");
                      var b = selfonly is bool self && self;
                      return new HandlerInfo(b, item, handler!);
                  });
                infos.AddRange(l1);
            }
            return [.. infos.Distinct(EqualityComparer<HandlerInfo>.Default)];
        }

        static List<INamedTypeSymbol> GetInterfacesIncludeBaseType(INamedTypeSymbol classSymbol)
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

        static AspectMemberContext GetMethodEnableHandlers(IMethodSymbol methodSymbol, INamedTypeSymbol owner)
        {
            var methodHandlers = methodSymbol.GetAttributes(AspectHandler).Select(a => a.GetNamedValue("AspectType")).OfType<INamedTypeSymbol>();
            if (!methodSymbol.GetAttribute(IgnoreAspect, out var ignoreInfo))
            {
                // 没有标注IgnoreAspect，直接返回所有可用的
                return new(methodSymbol, owner)
                {
                    MethodHandlers = [.. methodHandlers]
                };
            }
            _ = ignoreInfo!.GetConstructorValues(0, out var values);
            if (values.Length == 0)
            {
                // 标注了IgnoreAspect，但是没有指定忽略什么，则忽略全部，除了在自身标注的
                return new(methodSymbol, owner)
                {
                    IsIgnoreAll = true,
                };
            }
            // 指定忽略的类型
            return new(methodSymbol, owner)
            {
                MethodHandlers = [.. methodHandlers],
                IgnoreHandlers = [.. values.OfType<INamedTypeSymbol>()]
            };
        }

        static void ScanSymbolMethodsRecursive(INamedTypeSymbol owner
            , Action<AspectMemberContext> checkIsExplicit
            , HashSet<INamedTypeSymbol>? visitedSymbol = null)
        {
            visitedSymbol ??= new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
            // 防止循环引用
            if (!visitedSymbol.Add(owner))
                return;
            foreach (var m in owner.GetMembers())
            {
                if (m is IMethodSymbol method && method.MethodKind == MethodKind.Ordinary)
                {
                    var ctx = GetMethodEnableHandlers(method, owner);
                    ctx.Type = HandlerType.Method;
                    checkIsExplicit(ctx);
                }
                else if (m is IPropertySymbol prop)
                {
                    var ctx = new AspectMemberContext(prop, owner);
                    ctx.Type = HandlerType.Property;
                    checkIsExplicit(ctx);
                }
            }
            // 检查继承的接口
            var iparents = owner.GetInterfaces().ToArray();
            foreach (var i in iparents)
            {
                ScanSymbolMethodsRecursive(i, checkIsExplicit, visitedSymbol);
            }
        }

        static void CheckExplicit(List<AspectMemberContext> methodContexts, AspectMemberContext newCtx)
        {
            // 检查签名是否冲突
            for (int i = 0; i < methodContexts.Count; i++)
            {
                var other = methodContexts[i];
                if (newCtx.Type != other.Type)
                {
                    continue;
                }
                if (newCtx.Type == HandlerType.Method)
                {
                    if (newCtx.MethodSymbol.AreMethodsSignatureEqual(other.MethodSymbol))
                    {
                        newCtx.IsExplicit = true;
                        other.IsExplicit = true;
                    }
                }
                if (newCtx.Type == HandlerType.Property)
                {
                    if (newCtx.PropertySymbol.Name == other.PropertySymbol.Name)
                    {
                        newCtx.IsExplicit = true;
                        other.IsExplicit = true;
                    }
                }
            }
            methodContexts.Add(newCtx);
        }
    }

    public static CodeFile CreateCodeFile(GenerateContext context)
    {
        var classSymbol = context.TargetSymbol;
        var np = NamespaceBuilder.Default.Namespace(classSymbol.ContainingNamespace.ToDisplayString()).FileScoped();
        var proxyClass = ClassBuilder.Default;
        var allHandlers = context.AllHandlers;
        // 代理字段和aspect handler 字段
        List<Node> members = [
            FieldBuilder.Default.MemberType(classSymbol.ToDisplayString()).FieldName("proxy")
            , .. allHandlers.Select(n =>FieldBuilder.Default.MemberType(n.Type).FieldName(n.Name))
            ];
        // 构造函数
        List<Statement> ctorbody = [
            "this.proxy = proxy"
            , ..allHandlers.Select(n => $"this.{n.Name} = {n.Name}")
            ];
        var ctor = ConstructorBuilder.Default.MethodName($"{classSymbol.FormatClassName()}GeneratedProxy")
            .AddParameter([$"{classSymbol.ToDisplayString()} proxy", .. allHandlers.Select(n => $"{n.Type} {n.Name}")]).AddBody([.. ctorbody]);
        members.Add(ctor);

        foreach (var item in context.AllMethods)
        {
            //if (item.IsOverride)
            //{
            //    var node = CreateOverrideMember(context, item);
            //    members.Add(node);
            //    continue;
            //}
            if (item.Type == HandlerType.Method)
            {
                MethodBuilder methodBuilder;
                var methodHandlers = GetMethodEnableHandles(context, item);
                if (item.IsIgnoreAll || methodHandlers.Length == 0)
                {
                    methodBuilder = CreateDirectInvokeMethod(item);
                }
                else
                {
                    methodBuilder = CreateProxyMethod(context, item, methodHandlers);
                }
                members.Add(methodBuilder);
            }
            else if (item.Type == HandlerType.Property)
            {
                PropertyBuilder propertyBuilder = CreateProxyProperty(context, item);
                members.Add(propertyBuilder);
            }
        }

        var pc = proxyClass.ClassName($"{classSymbol.FormatClassName()}GeneratedProxy")
             .AddMembers([.. members])
             .Generic([.. classSymbol.GetTypeParameters()])
             .Interface([.. context.ProxyInterfaces.Select(i => i.ToDisplayString())])
             .AddGeneratedCodeAttribute(typeof(AutoAopProxyClassGenerator));

        //if (context.AttributeData.GetNamedValue<bool>("InheritBaseType", out var b) && b
        //    && classSymbol.BaseType is not null)
        //{
        //    pc.BaseType(classSymbol.BaseType.ToDisplayString());
        //}

        return CodeFile.New($"{classSymbol.FormatFileName()}GeneratedProxyClass.g.cs")
            .AddUsings("using AutoAopProxyGenerator;")
            .AddMembers(np.AddMembers(proxyClass));
    }

    private static MethodBuilder CreateDirectInvokeMethod(AspectMemberContext context)
    {
        var symbol = context.MethodSymbol;
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

    private static MethodBuilder CreateProxyMethod(GenerateContext gCtx, AspectMemberContext context, INamedTypeSymbol[] methodHandlers)
    {
        var methodSymbol = context.MethodSymbol;
        var cSymbol = gCtx.TargetSymbol;
        var method = methodSymbol.IsGenericMethod ? methodSymbol.ConstructedFrom : methodSymbol;
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
        statements.Add($"""
            var _context_gen = ContextHelper<{iSymbol.ToDisplayString()}, {cSymbol.ToDisplayString()}>.GetOrCreate("{method.Name}", {ptypes})
            """);

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

    private static PropertyBuilder CreateProxyProperty(GenerateContext gCtx, AspectMemberContext context)
    {
        var symbol = context.PropertySymbol;
        var iSymbol = context.DeclaredType;
        var pb = PropertyBuilder.Default.PropertyName(symbol.Name);
        pb.MemberType(symbol.Type.ToDisplayString());
        var obj = context.IsExplicit ? $"(({iSymbol.ToDisplayString()})proxy)" : "proxy";
        if (symbol.SetMethod is not null)
        {
            pb.SetLambda($"{obj}.{symbol.Name} = value");
        }
        else
        {
            pb.CanWrite = false;
        }
        if (symbol.GetMethod is not null)
        {
            pb.GetLambda($"{obj}.{symbol.Name}");
        }
        else
        {
            pb.CanRead = false;
        }
        if (context.IsExplicit)
        {
            pb.ExplicitFor(iSymbol.ToDisplayString());
        }
        return pb;
    }

    //private static Node CreateOverrideMember(GenerateContext gCtx, AspectMemberContext context)
    //{
    //    if (context.Type == HandlerType.Method)
    //    {
    //        var symbol = context.MethodSymbol;
    //        var m = MethodBuilder.Default;
    //        m.Modifiers(symbol.DeclaredAccessibility.ToString().ToLower());
    //        m.IsOverride = true;
    //        m.MethodName(symbol.Name)
    //            .Generic([.. symbol.GetTypeParameters()])
    //            .AddParameter([.. symbol.Parameters.Select(p => $"{p.Type.ToDisplayString()} {p.Name}")])
    //            .ReturnType(symbol.ReturnType.ToDisplayString())
    //            .AddGeneratedCodeAttribute(typeof(AutoAopProxyClassGenerator));
    //        m.Lambda($"proxy.{symbol.Name}({string.Join(", ", symbol.Parameters.Select(p => p.Name))})");
    //        return m;
    //    }
    //    else if (context.Type == HandlerType.Property)
    //    {
    //        var symbol = context.PropertySymbol;
    //        var p = PropertyBuilder.Default;
    //        p.Modifiers(symbol.DeclaredAccessibility.ToString().ToLower());
    //        p.PropertyName(symbol.Name);
    //        p.MemberType(symbol.Type.ToDisplayString());
    //        p.IsOverride = true;
    //        if (symbol.SetMethod is not null)
    //        {
    //            p.SetLambda($"proxy.{symbol.Name} = value");
    //        }
    //        else
    //        {
    //            p.CanWrite = false;
    //        }
    //        if (symbol.GetMethod is not null)
    //        {
    //            p.GetLambda($"proxy.{symbol.Name}");
    //        }
    //        else
    //        {
    //            p.CanRead = false;
    //        }
    //        return p;
    //    }
    //    throw new NotSupportedException();
    //}

    private static INamedTypeSymbol[] GetMethodEnableHandles(GenerateContext gCtx, AspectMemberContext mCtx)
    {
        var comparer = EqualityComparer<INamedTypeSymbol>.Default;
        HashSet<INamedTypeSymbol> enables = new(comparer);
        foreach (var item in gCtx.AllHandlers)
        {
            if (item.IsSelf && !comparer.Equals(item.DeclaredType, mCtx.DeclaredType))
            {
                continue;
            }
            if (mCtx.IgnoreHandlers.Length > 0 && mCtx.IgnoreHandlers.Contains(item.Handler, comparer))
            {
                continue;
            }
            enables.Add(item.Handler);
        }
        foreach (var item in mCtx.MethodHandlers)
        {
            if (mCtx.IgnoreHandlers.Length > 0 && mCtx.IgnoreHandlers.Contains(item, comparer))
            {
                continue;
            }
            enables.Add(item);
        }
        return [.. enables];
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
}
