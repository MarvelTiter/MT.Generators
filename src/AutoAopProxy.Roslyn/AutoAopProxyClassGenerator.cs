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


            var allInterfaces = targetSymbol.AllInterfaces.Where(a => a.HasAttribute(AspectHandler)).ToArray();

            #region 检查Attribute标注是否合法
            bool pass = true;
            foreach (var i in allInterfaces)
            {
                if (!pass)
                {
                    break;
                }
                var all = i.GetAttributes(AspectHandler);
                foreach (var a in all)
                {
                    var at = a.GetNamedValue("AspectType");
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
                }
            }
            if (!pass)
            {
                return;
            }
            #endregion


            #region 获取所有的AspectHandler
            var allHandlers = allInterfaces.SelectMany(x =>
            {
                var handlers = x.GetAttributes(AspectHandler).Select(a =>
                 {
                     var at = a.GetNamedValue("AspectType");
                     if (at == null)
                     {
                         context.ReportDiagnostic(DiagnosticDefinitions.AAPG00001(source.TargetNode.GetLocation()));
                         return null;
                     }
                     var att = (INamedTypeSymbol)at;
                     if (!att.HasInterface("AutoAopProxyGenerator.IAspectHandler"))
                     {
                         context.ReportDiagnostic(DiagnosticDefinitions.AAPG00002(source.TargetNode.GetLocation()));
                         return null;
                     }
                     return att;
                 });
                return handlers.Where(i => i != null).Cast<INamedTypeSymbol>();
            }).Distinct(EqualityComparer<INamedTypeSymbol>.Default).ToArray();
            #endregion
            var file = CreateGeneratedProxyClassFile(targetSymbol, allInterfaces, allHandlers);
            context.AddSource(file);
        });
    }

    private static CodeFile? CreateGeneratedProxyClassFile(INamedTypeSymbol classSymbol, INamedTypeSymbol[] interfaces, INamedTypeSymbol[] allHandlers)
    {
        List<MethodBuilder> methods = [];
        // 构造函数

        // 接口方法
        foreach (var iface in interfaces)
        {
            methods.AddRange(CreateProxyMethod(iface));
        }

        var ctor = ConstructorBuilder.Default.MethodName($"{classSymbol.FormatClassName()}GeneratedProxy");

        var proxyClass = ClassBuilder.Default.ClassName($"{classSymbol.FormatClassName()}GeneratedProxy");

        return CodeFile.New($"{classSymbol.FormatFileName()}GeneratedProxyClass.g.cs")
            .AddMembers(NamespaceBuilder.Default.Namespace(classSymbol.ContainingNamespace.ToDisplayString()).AddMembers(proxyClass));
    }

    private static IEnumerable<MethodBuilder> CreateProxyMethod(INamedTypeSymbol iface)
    {
        var handlers = iface.GetAttributes(AspectHandler);

        foreach (var m in iface.GetMethods())
        {

        }
        yield break;
    }
}
