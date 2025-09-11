using AutoPageStateContainerGenerator.Models;
using Generators.Models;
using Generators.Shared;
using Generators.Shared.Builder;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace AutoPageStateContainerGenerator;

internal class GeneratorHelper
{
    public const string STATE_CONTAINER = "AutoPageStateContainerGenerator.StateContainerAttribute";
    public const string STATE_FLAG = "AutoPageStateContainerGenerator.SaveStateAttribute";

    public static StateContext BuildContext(ISymbol target)
    {
        var classSymbol = (INamedTypeSymbol)target;
        classSymbol.GetAttribute(STATE_CONTAINER, out var attributeData);
        if (attributeData is null)
        {
            return new StateContext(classSymbol)
            {
                Diagnostic = Diagnostic.Create(new DiagnosticDescriptor("APSCG001", "StateContainerAttribute Error", "Cannot find StateContainerAttribute", "AutoPageStateContainerGenerator", DiagnosticSeverity.Error, true), classSymbol.Locations.FirstOrDefault())
            };
        }
        List<FieldMember> fields = [];
        List<PropertyMember> props = [];

        var fieldSymbols = classSymbol.GetMembers().Where(m => m.Kind == SymbolKind.Field && m.HasAttribute(STATE_FLAG)).Cast<IFieldSymbol>().ToArray();

        foreach (var field in fieldSymbols)
        {
            var syntax = field.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() as VariableDeclaratorSyntax;
            field.GetAttribute(STATE_FLAG, out var ad);
            var initString = ad.GetNamedValue("Init");
            var init = initString?.ToString() ?? syntax?.Initializer?.Value.ToFullString();
            fields.Add(new(field.Name, field.ContainingType, field.Type.ToDisplayString(), init));
        }

        var propSymbols = classSymbol.GetAllMembers(_ => true).Where(m => m.Kind == SymbolKind.Property && m.HasAttribute(STATE_FLAG)).Cast<IPropertySymbol>().ToArray();

        foreach (var prop in propSymbols)
        {
            var syntax = prop.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() as PropertyDeclarationSyntax;
            prop.GetAttribute(STATE_FLAG, out var ad);
            var initString = ad.GetNamedValue("Init");
            var init = initString?.ToString() ?? syntax?.Initializer?.Value.ToFullString();
            props.Add(new(prop.Name, prop.ContainingType, prop.Type.ToDisplayString(), prop.IsVirtual, init));
        }


        var lifetime = attributeData.GetNamedValue("Lifetime");
        var containerName = attributeData.GetNamedValue("Name") as string ?? "StateContainer";
        var implements = attributeData.GetNamedValue("Implements") as INamedTypeSymbol;
        return new StateContext(classSymbol)
        {
            Lifetime = lifetime,
            CustomName = containerName,
            Implements = implements,
            Fields = fields,
            Properties = props
        };
    }

    public static CodeFile CreateContainerClass(StateContext context)
    {
        var classSymbol = context.TargetSymbol;
        var lifetime = context.Lifetime;
        var containerName = context.CustomName;
        var implements = context.Implements;
        List<(string, string?)> attrParameters = [];
        if (lifetime is int v)
        {
            attrParameters.Add(("Lifetime", $"{v}"));
        }
        if (containerName is not null)
        {
            attrParameters.Add(("Name", $"\"{containerName}\""));
        }
        if (implements is not null)
        {
            attrParameters.Add(("Implements", $"typeof({implements.ToDisplayString()})"));
        }
        var attchAttr = "AutoPageStateContainerGenerator.GeneratedStateContainerAttribute";
        var attchInterface = "AutoPageStateContainerGenerator.IGeneratedStateContainer";
        var cb = ClassBuilder.Default
            .ClassName(context.TypeName)
            .Generic([.. classSymbol.GetTypeParameters()])
         .Attribute(attchAttr, [.. attrParameters])
            .Interface(attchInterface)
            .InterfaceIf(implements is not null, implements?.ToDisplayString()!)
            .AddGeneratedCodeAttribute(typeof(PageStateContainerGenerator));
        var changeEvent = FieldBuilder.Default.Modifiers("public").MemberType("event Action?").FieldName("OnChange");
        var notifyChangeMethod = MethodBuilder.Default.Modifiers("private").MethodName("NotifyStateChanged").Lambda("OnChange?.Invoke()");
        cb.AddMembers(changeEvent, notifyChangeMethod);
        foreach (var field in context.Fields)
        {
            var name = field.Name;
            if (name.StartsWith("_"))
            {
                name = name.Substring(1);
            }
            var propName = $"{name[0].ToString().ToUpper()}{name.Substring(1)}";
            var prop = PropertyBuilder.Default
                .PropertyName(propName)
                .MemberType(field.Type)
                .Full().Get(p =>
                {
                    return [$"return {p.FieldName}"];
                }).Set(p =>
                {
                    return [$"{p.FieldName} = value", "NotifyStateChanged()"];
                });
            if (field.Init is not null)
            {
                prop.InitializeWith(field.Init);
            }

            cb.AddMembers(prop);
        }

        foreach (var prop in context.Properties)
        {
            var pb = PropertyBuilder.Default
                .PropertyName(prop.Name)
                .MemberType(prop.Type)
                .Full().Get(p =>
                {
                    return [$"return {p.FieldName}"];
                }).Set(p =>
                {
                    return [$"{p.FieldName} = value", "NotifyStateChanged()"];
                });
            if (prop.Init is not null)
            {
                pb.InitializeWith(prop.Init);
            }
            cb.AddMembers(pb);
        }

        return CodeFile.New($"{classSymbol.FormatFileName()}.StateContainer.g.cs")
            .AddUsings(classSymbol.GetTargetUsings())
            .AddMembers(NamespaceBuilder.Default.FileScoped().Namespace(classSymbol.ContainingNamespace.ToDisplayString()).AddMembers(cb));
    }

    public static CodeFile CreatePartialRazorClass(StateContext context)
    {
        var classSymbol = context.TargetSymbol;
        var containerName = context.CustomName ?? "StateContainer";
        TypeParameterInfo[] typedArgs = [.. classSymbol.GetTypeParameters()];
        var cb = ClassBuilder.Default.ClassName($"{classSymbol.FormatClassName()}")
            .Generic(typedArgs)
            .Modifiers("partial");
        var sc = PropertyBuilder.Default
             .PropertyName(containerName)
             .MemberType(context.TypeName, typedArgs)
             .Attribute("global::Microsoft.AspNetCore.Components.InjectAttribute");
        cb.AddMembers(sc);
        foreach (var field in context.Fields)
        {
            var name = field.Name;
            if (name.StartsWith("_"))
            {
                name = name.Substring(1);
            }
            var propName = $"{name[0].ToString().ToUpper()}{name.Substring(1)}";
            var proxyProp = PropertyBuilder.Default
                .PropertyName(propName)
                .MemberType(field.Type)
                .GetLambda($"{containerName}.{propName}")
                .SetLambda($"{containerName}.{propName} = value");
            cb.AddMembers(proxyProp);
        }

        foreach (var prop in context.Properties)
        {
            if (!EqualityComparer<INamedTypeSymbol>.Default.Equals(prop.ContainingType, classSymbol) && !prop.IsVirtual)
            {
                continue;
            }
            var proxyProp = PropertyBuilder.Default
                .PropertyName(prop.Name)
                .Modifiers("public partial")
                .MemberType(prop.Type)
                .GetLambda($"{containerName}.{prop.Name}")
                .SetLambda($"{containerName}.{prop.Name} = value");
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

    public static CodeFile CreateServiceCollectionExtension(string @namespace, IEnumerable<StateContext> members)
    {
        var cb = ClassBuilder.Default
            .ClassName("PageStateContainerServiceCollectionExtensions")
            .Modifiers("public static");

        var mb = MethodBuilder.Default.MethodName("AddGeneratedContainerServices")
            .Modifiers("public static")
            .AddParameter("this IServiceCollection services");

        mb.AddBody("services.AddStateContainerManager()");

        foreach (var item in members)
        {
            mb.AddBody($"services.TryAdd(new ServiceDescriptor(typeof({item.TypeName}), typeof({item.TypeName}), {FormatLifetime(item.Lifetime)}))");
            if (item.Implements is not null)
            {
                mb.AddBody($"services.TryAdd(new ServiceDescriptor(typeof({item.Implements.ToDisplayString()}),(p) => p.GetRequiredService<{item.TypeName}>(), {FormatLifetime(item.Lifetime)}))");
            }
        }

        cb.AddMembers(mb);

        return CodeFile.New($"{@namespace}.PageState.g.cs")
            .AddUsings("using Microsoft.Extensions.DependencyInjection;")
            .AddUsings("using Microsoft.Extensions.DependencyInjection.Extensions;")
            .AddUsings("using AutoPageStateContainerGenerator;")
            .AddMembers(NamespaceBuilder.Default.FileScoped().Namespace(@namespace).AddMembers(cb));
    }

    private static string FormatLifetime(object? t)
    {
        return t switch
        {
            0 => "ServiceLifetime.Singleton",
            1 => "ServiceLifetime.Scoped",
            2 => "ServiceLifetime.Transient",
            _ => "ServiceLifetime.Scoped"
        };
    }
}
