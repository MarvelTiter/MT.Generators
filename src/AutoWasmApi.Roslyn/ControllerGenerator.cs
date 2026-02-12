using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Generators.Shared;
using Generators.Shared.Builder;
using Microsoft.CodeAnalysis;
using static AutoWasmApiGenerator.GeneratorHelpers;

namespace AutoWasmApiGenerator;

[Generator(LanguageNames.CSharp)]
public class ControllerGenerator : IIncrementalGenerator
{
    private const string PROXY_INSTANCE = "_proxyService_gen";
    private const string TUPLE_JSON_OPTION = "AutoWasmApiGenerator.AutoWasmApiGeneratorJsonHelper.TupleOption";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        //var ctx = context.SyntaxProvider.ForAttributeWithMetadataName(
        //    WebControllerAssemblyAttributeFullName,
        //    static (node, token) => true,
        //    static (c, t) => c);
        //var items = context.SyntaxProvider.ForAttributeWithMetadataName(
        //    WebControllerAttributeFullName
        //    , static (node, _) => true
        //    , static (ctx, _) => ctx);
#if DEBUG && false
        if (!Debugger.IsAttached)
        {
            Debugger.Launch(); // This will launch the debugger when the source generator runs.
        }
#endif

        context.RegisterSourceOutput(context.CompilationProvider, static (context, compilation) =>
        {
            try
            {
                if (!compilation.Assembly.HasAttribute(WebControllerAssemblyAttributeFullName))
                {
                    return;
                }

                var all = compilation.GetAllSymbols(WebControllerAttributeFullName);
                foreach (var item in all)
                {
                    if (!CreateCodeFile(item, context, out var file))
                        continue;
#if DEBUG
                    var ss = file.ToString();
#endif
                    context.AddSource(file);
                }
            }
            catch (Exception ex)
            {
                context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor(
                    id: "ControllerGenerator_ERROR00001",
                    title: "生成错误",
                    messageFormat: ex.Message,
                    category: typeof(ApiInvokerGenerator).FullName!,
                    defaultSeverity: DiagnosticSeverity.Warning,
                    isEnabledByDefault: true
                ), Location.None));
            }
        });
    }

    private static bool CreateCodeFile(INamedTypeSymbol interfaceSymbol, SourceProductionContext context,
        [NotNullWhen(true)] out CodeFile? file)
    {
        var methods = interfaceSymbol.GetAllMethodWithAttribute(WebMethodAttributeFullName).ToArray();
        if (methods.Any(a => (!a.Symbol.HasAttribute(ApiNotSupported) && !a.Symbol.HasAttribute(ControllerNotSupported)) && (a.Symbol.IsGenericMethod) || interfaceSymbol.IsGenericType))
        {
            file = null;
            context.ReportDiagnostic(DiagnosticDefinitions.WAG00004(interfaceSymbol.Locations.FirstOrDefault()));
            return false;
        }

        var ns = NamespaceBuilder.Default.Namespace(interfaceSymbol.ContainingNamespace.ToDisplayString());
        var controllerClass = CreateControllerClass(interfaceSymbol);
        List<Node> members = [];
        var localField = BuildLocalField(interfaceSymbol);
        var constructor = BuildConstructor(interfaceSymbol);
        members.Add(localField);
        members.Add(constructor);
        _ = interfaceSymbol.GetAttribute(WebControllerAttributeFullName, out var attributeData);
        var needAuth = attributeData.GetNamedValue("Authorize") ?? false;
        var authScheme = attributeData.GetNamedValue("AuthorizationScheme")?.ToString();
        foreach (var methodSymbol in methods)
        {
            if (methodSymbol.Symbol.HasAttribute(ControllerNotSupported))
            {
                continue;
            }

            var httpMethod = TryGetHttpMethod(methodSymbol);
            var methodSyntax = BuildMethod(methodSymbol, httpMethod, (bool)needAuth, authScheme);
            members.Add(methodSyntax);
        }

        file = CodeFile.New($"{interfaceSymbol.FormatFileName()}Controller.g.cs")
            .AddMembers(ns.AddMembers(controllerClass.AddMembers([.. members])));
        //.AddUsings(source.GetTargetUsings());

        return true;
    }

    private static MethodBuilder BuildMethod((IMethodSymbol, AttributeData?) data, string httpMethod, bool needAuth, string? authScheme)
    {
        /*
         * [global::Microsoft.AspNetCore.Mvc.{httpMethod}("...")]
         * [global::System.CodeDom.Compiler.GeneratedCode("...", "...")]
         * public <RETURN_TYPE> <METHOD_NAME>(<Parameter> p) => _proxyService_gen.<METHOD_NAME>(p);
         */
        var a = data.Item2;
        var methodSymbol = data.Item1;
        var methodScoped = methodSymbol.Name.Replace("Async", "");
        var customRoute = a?.GetNamedValue("Route")?.ToString();
        string methodRoute;
        if (string.IsNullOrEmpty(customRoute))
        {
            methodRoute = methodScoped;
        }
        else if (Regex.Match(customRoute, "{.+}").Success)
        {
            methodRoute = $"{methodScoped}/{customRoute}";
        }
        else
        {
            methodRoute = customRoute!;
        }

        var methodRouteAttribute =
            $"global::Microsoft.AspNetCore.Mvc.Http{httpMethod}(\"{methodRoute}\")";
        var allowAnonymous = (bool)(a?.GetNamedValue("AllowAnonymous") ?? false);
        var methodAuth = (bool)(a?.GetNamedValue("Authorize") ?? false);
        var authorizeAttribute = string.IsNullOrEmpty(authScheme)
            ? "global::Microsoft.AspNetCore.Authorization.Authorize"
            : $"""global::Microsoft.AspNetCore.Authorization.Authorize(AuthenticationSchemes = "{authScheme}")""";
        var (IsTask, HasReturn, ReturnType) = methodSymbol.GetReturnTypeInfo();
        if (HasReturn && ReturnType.IsTupleType)
        {
            var methodReturn = IsTask ? "global::System.Threading.Tasks.Task<string>" : "object";
            var methodReturnResult = $"var _return_gen = {(IsTask ? "await " : "")}{PROXY_INSTANCE}.{methodSymbol.Name}({string.Join(", ", methodSymbol.Parameters.Select(p => p.Name))})";
            var tuple = (INamedTypeSymbol)ReturnType;
            StringBuilder obj = new();
            ConvertToAnonymousObject(obj, tuple, "_return_gen");
            var finalReturn = $"var _anonymous_gen = {obj};";
            var finalJsonReturn = $"return global::System.Text.Json.JsonSerializer.Serialize(_anonymous_gen, {TUPLE_JSON_OPTION})";
            return MethodBuilder.Default
                .MethodName(methodSymbol.Name)
                .Async(IsTask)
                .ReturnType(methodReturn)
                .Attribute(methodRouteAttribute)
                .AttributeIf(allowAnonymous, "global::Microsoft.AspNetCore.Authorization.AllowAnonymous")
                .AttributeIf((methodAuth || needAuth) && !allowAnonymous, authorizeAttribute)
                .AddGeneratedCodeAttribute(typeof(ControllerGenerator))
                .AddParameter(GenerateParameter(httpMethod, methodSymbol))
                .AddBody(methodReturnResult, finalReturn, finalJsonReturn);
        }
        else
        {
            return MethodBuilder.Default
                .MethodName(methodSymbol.Name)
                .ReturnType(methodSymbol.ReturnType.ToDisplayString())
                .Attribute(methodRouteAttribute)
                .AttributeIf(allowAnonymous, "global::Microsoft.AspNetCore.Authorization.AllowAnonymous")
                .AttributeIf((methodAuth || needAuth) && !allowAnonymous, authorizeAttribute)
                .AddGeneratedCodeAttribute(typeof(ControllerGenerator))
                .AddParameter(GenerateParameter(httpMethod, methodSymbol))
                .Lambda(
                    $"{PROXY_INSTANCE}.{methodSymbol.Name}({string.Join(", ", methodSymbol.Parameters.Select(p => p.Name))});");
        }
    }

    private static void ConvertToAnonymousObject(StringBuilder obj, INamedTypeSymbol tuple, string tupleObject)
    {
        obj.Append("new {");
        foreach (var field in tuple.TupleElements)
        {
            obj.Append($"{field.Name}=");
            if (field.Type.IsTupleType)
            {
                var subTupleObject = $"{tupleObject}.{field.Name}";
                ConvertToAnonymousObject(obj, (INamedTypeSymbol)field.Type, subTupleObject);
            }
            else
            {
                obj.Append($"{tupleObject}.{field.Name},");
            }
        }
        obj.Append('}');
    }

    private static string[] GenerateParameter(string httpMethod, IMethodSymbol methodSymbol)
    {
        var parametersBinding = methodSymbol.Parameters.Select(p =>
        {
            // 忽略CancellationToken
            if (p.Type.ToString() == typeof(CancellationToken).FullName)
            {
                return (-1, p);
            }

            if (!p.GetAttribute(GeneratorHelpers.WebMethodParameterBindingAttribute, out var binding))
            {
                // 如果参数值是自定义类，默认使用body传参，否则使用query传参
                if (p.Type is { TypeKind: TypeKind.Class, SpecialType: not SpecialType.System_String })
                {
                    return (3, p);
                }

                return (0, p);
            }

            Debug.Assert(binding != null, nameof(binding) + " != null");
            if (!binding!.GetConstructorValue(0, out var bindingType))
            {
                return (-1, p);
            }

            return ((int)bindingType!, p);
        }).ToList();

        var enumerable = parametersBinding.Select(b =>
        {
            var (bindingType, p) = b;
            return $"{CreateMethodParameterBinding(bindingType, httpMethod)}{p.Type.ToDisplayString()} {p.Name}";
        });
        return enumerable.ToArray();

        static string CreateMethodParameterOriginAttribute(string method)
        {
            return method switch
            {
                "Get" => "[global::Microsoft.AspNetCore.Mvc.FromQuery]",
                "Post" => "[global::Microsoft.AspNetCore.Mvc.FromBody]",
                "Put" => "[global::Microsoft.AspNetCore.Mvc.FromBody]",
                "Delete" => "[global::Microsoft.AspNetCore.Mvc.FromBody]",
                _ => throw new NotImplementedException()
            };
        }

        static string CreateMethodParameterBinding(int bindingType, string method)
        {
            return bindingType switch
            {
                0 => "[global::Microsoft.AspNetCore.Mvc.FromQuery]", // FromQuery
                1 => "[global::Microsoft.AspNetCore.Mvc.FromRoute]", // FromRoute
                2 => "[global::Microsoft.AspNetCore.Mvc.FromForm]", // FromForm
                3 => "[global::Microsoft.AspNetCore.Mvc.FromBody]", // FromBody
                4 => "[global::Microsoft.AspNetCore.Mvc.FromHeader]", // FromHeader
                5 => "[global::Microsoft.AspNetCore.Mvc.FromServices]", // FromServices
                -1 => "", // Ignore
                _ => CreateMethodParameterOriginAttribute(method), // Fall back to default
            };
        }
    }

    private static FieldBuilder BuildLocalField(INamedTypeSymbol interfaceSymbol)
    {
        // private readonly <SERVICE_TYPE> _proxyService_gen;
        return FieldBuilder.Default
            .MemberType(interfaceSymbol.ToDisplayString())
            .FieldName(PROXY_INSTANCE);
    }

    private static ConstructorBuilder BuildConstructor(INamedTypeSymbol interfaceSymbol)
    {
        /*
         * public <SERVICE_TYPE>Controller(<SERVICE_TYPE> service)
         * {
         *     _proxyService_gen = service;
         * }
         */

        return ConstructorBuilder.Default
            .MethodName($"{FormatClassName(interfaceSymbol.FormatClassName())}Controller")
            .AddGeneratedCodeAttribute(typeof(ControllerGenerator))
            .AddBody($"{PROXY_INSTANCE} = service;")
            .AddParameter($"{interfaceSymbol.ToDisplayString()} service");
    }

    private static ClassBuilder CreateControllerClass(INamedTypeSymbol interfaceSymbol)
    {
        _ = interfaceSymbol.GetAttribute(WebControllerAttributeFullName, out var controllerAttribute);
        var route = controllerAttribute.GetNamedValue("Route") ?? "[controller]";
        var needAuth = controllerAttribute.GetNamedValue("Authorize") ?? false;
        //var additionalAttribute = source.TargetSymbol.GetAttributeInitInfo<ControllerGenerator>();
        var attchAttribute = "AutoWasmApiGenerator.Attributes.GeneratedByAutoWasmApiGeneratorAttribute";
        (string, string?)[] attrParams = [
            ("InterfaceType", $"typeof({interfaceSymbol.ToDisplayString()})"),
            ("Part", "AutoWasmApiGenerator.Attributes.PartType.Controller")
            ];
        return ClassBuilder.Default
            .ClassName($"{FormatClassName(interfaceSymbol.FormatClassName())}Controller")
            .Modifiers("public")
            .BaseType("global::Microsoft.AspNetCore.Mvc.ControllerBase")
            .Attribute("global::Microsoft.AspNetCore.Mvc.ApiController")
            .Attribute($"global::Microsoft.AspNetCore.Mvc.Route(\"api/{route}\")")
            .AttributeIf((bool)needAuth, "global::Microsoft.AspNetCore.Authorization.Authorize")
            //.Attribute([..additionalAttribute.Select(i => i.ToString())])
            .AddGeneratedCodeAttribute(typeof(ControllerGenerator))
            .Attribute(attchAttribute, attrParams);
    }

    private static string TryGetHttpMethod((IMethodSymbol, AttributeData?) data)
    {
        if (data.Item2 != null)
        {
            if (data.Item2.GetNamedValue("Method", out var m))
            {
                return WebMethod[(int)m!];
            }
        }

        //没有指定Method，就默认Post
        return WebMethod[1];

        //var symbol = data.Item1;
        ////if (symbol.Parameters.Any(p =>p.Type.isr))

        //var name = symbol.Name;
        //if (name.StartsWith("create", StringComparison.OrdinalIgnoreCase)
        //    || name.StartsWith("add", StringComparison.OrdinalIgnoreCase))
        //{
        //    return WebMethod[1];
        //}
        //else if (name.StartsWith("get", StringComparison.OrdinalIgnoreCase)
        //    || name.StartsWith("find", StringComparison.OrdinalIgnoreCase)
        //    || name.StartsWith("query", StringComparison.OrdinalIgnoreCase))
        //{
        //    return WebMethod[0];
        //}
        //else if (name.StartsWith("update", StringComparison.OrdinalIgnoreCase)
        //    || name.StartsWith("put", StringComparison.OrdinalIgnoreCase))
        //{
        //    return WebMethod[2];
        //}
        //else if (name.StartsWith("delete", StringComparison.OrdinalIgnoreCase)
        //    || name.StartsWith("remove", StringComparison.OrdinalIgnoreCase))
        //{
        //    return WebMethod[3];
        //}
        //else
        //{
        //    return WebMethod[1];
        //}
    }
}