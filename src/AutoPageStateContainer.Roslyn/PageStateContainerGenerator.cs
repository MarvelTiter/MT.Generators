using Generators.Shared;
using Generators.Shared.Builder;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoPageStateContainerGenerator;

[Generator(LanguageNames.CSharp)]
public class PageStateContainerGenerator : IIncrementalGenerator
{
    const string STATE_CONTAINER = "AutoPageStateContainerGenerator.StateContainerAttribute";
    const string STATE_FLAG = "AutoPageStateContainerGenerator.SaveStateAttribute";
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var map = context.SyntaxProvider.ForAttributeWithMetadataName(
             STATE_CONTAINER,
             static (node, _) => node is ClassDeclarationSyntax,
             static (ctx, _) => ctx);

        context.RegisterSourceOutput(map, CreateCodeFile);
    }

    private static void CreateCodeFile(SourceProductionContext context, GeneratorAttributeSyntaxContext source)
    {
        var classSymbol = (INamedTypeSymbol)source.TargetSymbol;
        var fields = classSymbol.GetMembers().Where(m => m.Kind == SymbolKind.Field && m.HasAttribute(STATE_FLAG)).Cast<IFieldSymbol>().ToArray();

        var props = classSymbol.GetAllMembers(_ => true).Where(m => m.Kind == SymbolKind.Property && m.HasAttribute(STATE_FLAG)).Cast<IPropertySymbol>().ToArray();
        // 创建容器类

        // TODO 检查字段命名

        var containerClass = CreateContainerClass(classSymbol, fields, props);
        context.AddSource(containerClass);
        // 创建Razor组件分部类
        var partialRazor = CreatePartialRazorClass(classSymbol, fields, props);
        context.AddSource(partialRazor);
    }

    private static CodeFile CreateContainerClass(INamedTypeSymbol classSymbol, IFieldSymbol[] fields, IPropertySymbol[] props)
    {
        classSymbol.GetAttribute(STATE_CONTAINER, out var attributeData);
        var lifetime = attributeData.GetNamedValue("Lifetime");
        string generatedAttribute;
        if (lifetime is int v)
        {
            generatedAttribute = $"""
                AutoPageStateContainerGenerator.GeneratedStateContainerAttribute( Lifetime = {v})
                """;

        }
        else
        {
            generatedAttribute = "AutoPageStateContainerGenerator.GeneratedStateContainerAttribute";
        }
        var cb = ClassBuilder.Default
            .ClassName($"{classSymbol.FormatClassName(true)}StateContainer")
            .Attribute(generatedAttribute)
            .Interface("AutoPageStateContainerGenerator.IGeneratedStateContainer")
            .AddGeneratedCodeAttribute(typeof(PageStateContainerGenerator));
        var changeEvent = FieldBuilder.Default.Modifiers("public").MemberType("event Action?").FieldName("OnChange");
        var notifyChangeMethod = MethodBuilder.Default.Modifiers("private").MethodName("NotifyStateChanged").Lambda("OnChange?.Invoke()");
        cb.AddMembers(changeEvent, notifyChangeMethod);
        foreach (var field in fields)
        {
            var syntax = field.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() as VariableDeclaratorSyntax;

            var init = syntax?.Initializer?.Value;

            field.GetAttribute(STATE_FLAG, out var ad);
            var initString = ad.GetNamedValue("Init");

            var name = field.Name;
            if (name.StartsWith("_"))
            {
                name = name.Substring(1);
            }
            var propName = $"{name[0].ToString().ToUpper()}{name.Substring(1)}";
            var prop = PropertyBuilder.Default
                .PropertyName(propName)
                .MemberType(field.Type.ToDisplayString())
                .Full().Get(p =>
             {
                 return [$"return {p.FieldName}"];
             }).Set(p =>
             {
                 return [$"{p.FieldName} = value", "NotifyStateChanged()"];
             });
            if (init is not null)
            {
                prop.InitializeWith(init.ToFullString());
            }

            if (initString is string s && s.Length > 0)
            {
                prop.InitializeWith(s);
            }
            cb.AddMembers(prop);
        }

        foreach (var prop in props)
        {
            var syntax = prop.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() as PropertyDeclarationSyntax;
            var init = syntax?.Initializer?.Value;
            prop.GetAttribute(STATE_FLAG, out var ad);
            var initString = ad.GetNamedValue("Init");
            var pb = PropertyBuilder.Default
                .PropertyName(prop.Name)
                .MemberType(prop.Type.ToDisplayString())
                .Full().Get(p =>
                {
                    return [$"return {p.FieldName}"];
                }).Set(p =>
                {
                    return [$"{p.FieldName} = value", "NotifyStateChanged()"];
                });
            if (init is not null)
            {
                pb.InitializeWith(init.ToFullString());
            }
            if (initString is string s && s.Length > 0)
            {
                pb.InitializeWith(s);
            }
            cb.AddMembers(pb);
        }

        return CodeFile.New($"{classSymbol.FormatFileName()}.StateContainer.g.cs")
            .AddUsings(classSymbol.GetTargetUsings())
            .AddMembers(NamespaceBuilder.Default.FileScoped().Namespace(classSymbol.ContainingNamespace.ToDisplayString()).AddMembers(cb));
    }

    private static CodeFile CreatePartialRazorClass(INamedTypeSymbol classSymbol, IFieldSymbol[] fields, IPropertySymbol[] props)
    {
        var cb = ClassBuilder.Default.ClassName($"{classSymbol.FormatClassName()}")
            .Modifiers("partial");
        var sc = PropertyBuilder.Default
             .PropertyName("StateContainer")
             .MemberType($"{classSymbol.FormatClassName(true)}StateContainer")
             .Attribute("global::Microsoft.AspNetCore.Components.InjectAttribute");
        cb.AddMembers(sc);
        foreach (var field in fields)
        {
            var name = field.Name;
            if (name.StartsWith("_"))
            {
                name = name.Substring(1);
            }
            var propName = $"{name[0].ToString().ToUpper()}{name.Substring(1)}";
            var proxyProp = PropertyBuilder.Default
                .PropertyName(propName)
                .MemberType(field.Type.ToDisplayString())
                .GetLambda($"StateContainer.{propName}")
                .SetLambda($"StateContainer.{propName} = value");
            cb.AddMembers(proxyProp);
        }

        foreach (var prop in props)
        {
            if (!EqualityComparer<INamedTypeSymbol>.Default.Equals(prop.ContainingType, classSymbol) && !prop.IsVirtual)
            {
                continue;
            }
            var proxyProp = PropertyBuilder.Default
                .PropertyName(prop.Name)
                .Modifiers("public partial")
                .MemberType(prop.Type.ToDisplayString())
                .GetLambda($"StateContainer.{prop.Name}")
                .SetLambda($"StateContainer.{prop.Name} = value");
            if (prop.IsVirtual)
            {
                proxyProp.Modifiers("public override");
            }
            cb.AddMembers(proxyProp);
        }

        return CodeFile.New($"{classSymbol.FormatFileName()}.PageState.g.cs")
            .AddUsings(classSymbol.GetTargetUsings())
            .AddMembers(NamespaceBuilder.Default.FileScoped().Namespace(classSymbol.ContainingNamespace.ToDisplayString()).AddMembers(cb));
    }
}
