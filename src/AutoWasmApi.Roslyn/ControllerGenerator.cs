using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using AutoWasmApiGenerator.Models;
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
        context.RegisterSourceOutput(context.CompilationProvider, static (context, compilation) =>
        {
            try
            {
                if (!compilation.Assembly.HasAttribute(WebControllerAssemblyAttributeFullName))
                {
                    return;
                }
                compilation.Assembly.GetAttribute(WebControllerAssemblyAttributeFullName, out var apiType);
                object? mode = null;
                apiType?.GetNamedValue("Mode", out mode);
                var isController = mode is int m && m == 1;
                var all = compilation.GetAllSymbols(WebControllerAttributeFullName);
                List<string> endpoints = [];
                foreach (var item in all)
                {
                    CodeFile? file;
                    var (apiContext, error) = CollectApiContext(item);
                    if (error is not null)
                    {
                        context.ReportDiagnostic(error);
                        continue;
                    }
                    if (isController)
                    {
                        file = CreateControllerCodeFile(apiContext);
                    }
                    else
                    {
                        file = CreateMinimalCodeFile(apiContext, out var ep);
                        endpoints.Add(ep);
                    }
#if DEBUG
                    var ss = file.ToString();
#endif
                    context.AddSource(file);
                }
                if (endpoints.Count > 0)
                {
                    var allEndpoints = string.Join(", ", endpoints.Select(e => $"\"{e}\""));
                    var np = NamespaceBuilder.Default.Namespace("AutoWasmApiGenerator").FileScoped();
                    var c = ClassBuilder.Default.ClassName("AutoWasmApiEndPointsExtensions")
                        .Modifiers("public static")
                        .AddGeneratedCodeAttribute(typeof(ControllerGenerator))
                        .AddMembers(MethodBuilder.Default.MethodName("MapAutoWasmApiEndPoints").Modifiers("public static")
                            .AddParameter("this WebApplication app")
                            .AddBody([.. endpoints.Select(e => $"{e}(app)")]));
                    var af = CodeFile.New("AutoWasmApiEndPointsExtensions")
                        .AddUsings("using Microsoft.AspNetCore.Builder;")
                        .AddMembers(np.AddMembers(c));
                    context.AddSource(af);
                }
            }
            catch (Exception ex)
            {
                context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor(
                    id: "ControllerGenerator_ERROR00001",
                    title: "生成错误",
                    messageFormat: ex.Message,
                    category: typeof(ApiClientGenerator).FullName!,
                    defaultSeverity: DiagnosticSeverity.Warning,
                    isEnabledByDefault: true
                ), Location.None));
            }
        });
    }

    private static (ApiContext apiContext, Diagnostic? error) CollectApiContext(INamedTypeSymbol interfaceSymbol)
    {
        var methods = interfaceSymbol.GetAllMethodWithAttribute(WebMethodAttributeFullName).ToArray();
        if (methods.Any(a => (!a.Symbol.HasAttribute(ApiNotSupported) && !a.Symbol.HasAttribute(ControllerNotSupported)) && (a.Symbol.IsGenericMethod) || interfaceSymbol.IsGenericType))
        {
            var dia = DiagnosticDefinitions.WAG00004(interfaceSymbol.Locations.FirstOrDefault());
            return (null!, dia);
        }
        _ = interfaceSymbol.GetAttribute(WebControllerAttributeFullName, out var attributeData);
        var needAuth = (bool)(attributeData.GetNamedValue("Authorize") ?? false);
        var authScheme = attributeData.GetNamedValue("Scheme")?.ToString();
        var route = attributeData.GetNamedValue("Route")?.ToString();
        var authorize = new AuthorizeInfo()
        {
            RequiredAuthorize = needAuth,
            AuthorizeScheme = authScheme,
        };
        List<ApiMethodInfo> apiMethods = [];
        foreach (var (symbol, attrData) in methods)
        {
            if (symbol.HasAttribute(ControllerNotSupported))
            {
                continue;
            }

            var httpMethod = TryGetHttpMethod(attrData);
            var methodScoped = symbol.Name.Replace("Async", "");
            var customRoute = attrData?.GetNamedValue("Route")?.ToString();
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
            var allowAnonymous = (bool)(attrData?.GetNamedValue("AllowAnonymous") ?? false);
            var methodAuth = (bool)(attrData?.GetNamedValue("Authorize") ?? false);
            var ami = new ApiMethodInfo(symbol)
            {
                RouteUrl = methodRoute,
                HttpMethod = httpMethod,
                MethodAuthorize = new()
                {
                    AllowAnonymous = allowAnonymous,
                    RequiredAuthorize = methodAuth
                }
            };
            apiMethods.Add(ami);
        }

        return (new(interfaceSymbol)
        {
            RouteUrl = route,
            AuthorizeInfo = authorize,
            Methods = apiMethods
        }, null);
    }

    #region 
    //private static bool CreateControllerCodeFile(INamedTypeSymbol interfaceSymbol, SourceProductionContext context,
    //    [NotNullWhen(true)] out CodeFile? file)
    //{
    //    //var interfaceSymbol = context.InterfaceType;
    //    var methods = interfaceSymbol.GetAllMethodWithAttribute(WebMethodAttributeFullName).ToArray();
    //    if (methods.Any(a => (!a.Symbol.HasAttribute(ApiNotSupported) && !a.Symbol.HasAttribute(ControllerNotSupported)) && (a.Symbol.IsGenericMethod) || interfaceSymbol.IsGenericType))
    //    {
    //        file = null;
    //        context.ReportDiagnostic(DiagnosticDefinitions.WAG00004(interfaceSymbol.Locations.FirstOrDefault()));
    //        return false;
    //    }

    //    var ns = NamespaceBuilder.Default.Namespace(interfaceSymbol.ContainingNamespace.ToDisplayString());
    //    var controllerClass = CreateControllerClass(interfaceSymbol);
    //    List<Node> members = [];
    //    var localField = BuildLocalField(interfaceSymbol);
    //    var constructor = BuildConstructor(interfaceSymbol);
    //    members.Add(localField);
    //    members.Add(constructor);
    //    _ = interfaceSymbol.GetAttribute(WebControllerAttributeFullName, out var attributeData);
    //    var needAuth = attributeData.GetNamedValue("Authorize") ?? false;
    //    var authScheme = attributeData.GetNamedValue("AuthorizationScheme")?.ToString();
    //    foreach (var methodSymbol in methods)
    //    {
    //        if (methodSymbol.Symbol.HasAttribute(ControllerNotSupported))
    //        {
    //            continue;
    //        }

    //        var httpMethod = TryGetHttpMethod(methodSymbol.AttrData);
    //        var methodSyntax = BuildMethod(methodSymbol, httpMethod, (bool)needAuth, authScheme);
    //        members.Add(methodSyntax);
    //    }

    //    file = CodeFile.New($"{interfaceSymbol.FormatFileName()}Controller.g.cs")
    //        .AddMembers(ns.AddMembers(controllerClass.AddMembers([.. members])));
    //    //.AddUsings(source.GetTargetUsings());

    //    return true;

    //    static MethodBuilder BuildMethod((IMethodSymbol, AttributeData?) data, string httpMethod, bool needAuth, string? authScheme)
    //    {
    //        /*
    //         * [global::Microsoft.AspNetCore.Mvc.{httpMethod}("...")]
    //         * [global::System.CodeDom.Compiler.GeneratedCode("...", "...")]
    //         * public <RETURN_TYPE> <METHOD_NAME>(<Parameter> p) => _proxyService_gen.<METHOD_NAME>(p);
    //         */
    //        var a = data.Item2;
    //        var methodSymbol = data.Item1;
    //        var methodScoped = methodSymbol.Name.Replace("Async", "");
    //        var customRoute = a?.GetNamedValue("Route")?.ToString();
    //        string methodRoute;
    //        if (string.IsNullOrEmpty(customRoute))
    //        {
    //            methodRoute = methodScoped;
    //        }
    //        else if (Regex.Match(customRoute, "{.+}").Success)
    //        {
    //            methodRoute = $"{methodScoped}/{customRoute}";
    //        }
    //        else
    //        {
    //            methodRoute = customRoute!;
    //        }

    //        var methodRouteAttribute =
    //            $"global::Microsoft.AspNetCore.Mvc.Http{httpMethod}(\"{methodRoute}\")";
    //        var allowAnonymous = (bool)(a?.GetNamedValue("AllowAnonymous") ?? false);
    //        var methodAuth = (bool)(a?.GetNamedValue("Authorize") ?? false);
    //        var authorizeAttribute = string.IsNullOrEmpty(authScheme)
    //            ? "global::Microsoft.AspNetCore.Authorization.Authorize"
    //            : $"""global::Microsoft.AspNetCore.Authorization.Authorize(AuthenticationSchemes = "{authScheme}")""";
    //        var (IsTask, HasReturn, ReturnType) = methodSymbol.GetReturnTypeInfo();
    //        if (HasReturn && ReturnType.IsTupleType)
    //        {
    //            var methodReturn = IsTask ? "global::System.Threading.Tasks.Task<string>" : "object";
    //            var methodReturnResult = $"var _return_gen = {(IsTask ? "await " : "")}{PROXY_INSTANCE}.{methodSymbol.Name}({string.Join(", ", methodSymbol.Parameters.Select(p => p.Name))})";
    //            var tuple = (INamedTypeSymbol)ReturnType;
    //            StringBuilder obj = new();
    //            ConvertToAnonymousObject(obj, tuple, "_return_gen");
    //            var finalReturn = $"var _anonymous_gen = {obj};";
    //            var finalJsonReturn = $"return global::System.Text.Json.JsonSerializer.Serialize(_anonymous_gen, {TUPLE_JSON_OPTION})";
    //            return MethodBuilder.Default
    //                .MethodName(methodSymbol.Name)
    //                .Async(IsTask)
    //                .ReturnType(methodReturn)
    //                .Attribute(methodRouteAttribute)
    //                .AttributeIf(allowAnonymous, "global::Microsoft.AspNetCore.Authorization.AllowAnonymous")
    //                .AttributeIf((methodAuth || needAuth) && !allowAnonymous, authorizeAttribute)
    //                .AddGeneratedCodeAttribute(typeof(ControllerGenerator))
    //                .AddParameter(GenerateParameter(httpMethod, methodSymbol))
    //                .AddBody(methodReturnResult, finalReturn, finalJsonReturn);
    //        }
    //        else
    //        {
    //            return MethodBuilder.Default
    //                .MethodName(methodSymbol.Name)
    //                .ReturnType(methodSymbol.ReturnType.ToDisplayString())
    //                .Attribute(methodRouteAttribute)
    //                .AttributeIf(allowAnonymous, "global::Microsoft.AspNetCore.Authorization.AllowAnonymous")
    //                .AttributeIf((methodAuth || needAuth) && !allowAnonymous, authorizeAttribute)
    //                .AddGeneratedCodeAttribute(typeof(ControllerGenerator))
    //                .AddParameter(GenerateParameter(httpMethod, methodSymbol))
    //                .Lambda(
    //                    $"{PROXY_INSTANCE}.{methodSymbol.Name}({string.Join(", ", methodSymbol.Parameters.Select(p => p.Name))});");
    //        }
    //    }
    //}

    #endregion

    #region controller

    private static CodeFile CreateControllerCodeFile(ApiContext context)
    {
        var methods = context.Methods;
        var ns = NamespaceBuilder.Default.Namespace(context.NameSpace);
        var controllerClass = CreateControllerClass(context);
        List<Node> members = [];
        var localField = BuildLocalField(context);
        var constructor = BuildConstructor(context);
        members.Add(localField);
        members.Add(constructor);
        //var needAuth = context.AuthorizeInfo is not null;
        //var authScheme = context.AuthorizeInfo?.AuthorizeScheme;
        foreach (var ami in methods)
        {
            var methodSyntax = BuildMethod(context, ami);
            members.Add(methodSyntax);
        }

        var file = CodeFile.New($"{context.InterfaceType.FormatFileName()}Controller.g.cs")
             .AddMembers(ns.AddMembers(controllerClass.AddMembers([.. members])));
        //.AddUsings(source.GetTargetUsings());

        return file;

        static ClassBuilder CreateControllerClass(ApiContext apiContext)
        {
            var route = apiContext.RouteUrl ?? "[controller]";
            var needAuth = apiContext.AuthorizeInfo.RequiredAuthorize;
            //var additionalAttribute = source.TargetSymbol.GetAttributeInitInfo<ControllerGenerator>();
            var attchAttribute = "AutoWasmApiGenerator.Attributes.GeneratedByAutoWasmApiGeneratorAttribute";
            (string, string?)[] attrParams = [
                ("InterfaceType", $"typeof({apiContext.TypeName})"),
            ("Part", "AutoWasmApiGenerator.Attributes.PartType.Controller")
                ];
            return ClassBuilder.Default
                .ClassName($"{FormatClassName(apiContext.FileName)}Controller")
                .Modifiers("public")
                .BaseType("global::Microsoft.AspNetCore.Mvc.ControllerBase")
                .Attribute("global::Microsoft.AspNetCore.Mvc.ApiController")
                .Attribute($"global::Microsoft.AspNetCore.Mvc.Route(\"api/{route}\")")
                .AttributeIf((bool)needAuth, "global::Microsoft.AspNetCore.Authorization.Authorize")
                //.Attribute([..additionalAttribute.Select(i => i.ToString())])
                .AddGeneratedCodeAttribute(typeof(ControllerGenerator))
                .Attribute(attchAttribute, attrParams);
        }

        static FieldBuilder BuildLocalField(ApiContext apiContext)
        {
            // private readonly <SERVICE_TYPE> _proxyService_gen;
            return FieldBuilder.Default
                .MemberType(apiContext.TypeName)
                .FieldName(PROXY_INSTANCE);
        }

        static ConstructorBuilder BuildConstructor(ApiContext apiContext)
        {
            /*
             * public <SERVICE_TYPE>Controller(<SERVICE_TYPE> service)
             * {
             *     _proxyService_gen = service;
             * }
             */
            
            return ConstructorBuilder.Default
                .MethodName($"{FormatClassName(apiContext.FileName)}Controller")
                .AddGeneratedCodeAttribute(typeof(ControllerGenerator))
                .AddBody($"{PROXY_INSTANCE} = service;")
                .AddParameter($"{apiContext.TypeName} service");
        }

        static MethodBuilder BuildMethod(ApiContext api, ApiMethodInfo method)
        {
            /*
             * [global::Microsoft.AspNetCore.Mvc.{httpMethod}("...")]
             * [global::System.CodeDom.Compiler.GeneratedCode("...", "...")]
             * public <RETURN_TYPE> <METHOD_NAME>(<Parameter> p) => _proxyService_gen.<METHOD_NAME>(p);
             */
            string methodRoute = method.RouteUrl;
            string httpMethod = method.HttpMethod;
            var methodRouteAttribute =
                $"global::Microsoft.AspNetCore.Mvc.Http{httpMethod}(\"{methodRoute}\")";
            var allowAnonymous = method.MethodAuthorize.AllowAnonymous;
            var methodAuth = method.MethodAuthorize.RequiredAuthorize;
            var apiAuth = api.AuthorizeInfo.RequiredAuthorize;
            var apiAuthScheme = api.AuthorizeInfo.AuthorizeScheme;
            var methodSymbol = method.Symbol;
            var authorizeAttribute = string.IsNullOrEmpty(apiAuthScheme)
                ? "global::Microsoft.AspNetCore.Authorization.Authorize"
                : $"""global::Microsoft.AspNetCore.Authorization.Authorize(AuthenticationSchemes = "{apiAuthScheme}")""";
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
                    .AttributeIf((methodAuth || apiAuth) && !allowAnonymous, authorizeAttribute)
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
                    .AttributeIf((methodAuth || apiAuth) && !allowAnonymous, authorizeAttribute)
                    .AddGeneratedCodeAttribute(typeof(ControllerGenerator))
                    .AddParameter(GenerateParameter(httpMethod, methodSymbol))
                    .Lambda(
                        $"{PROXY_INSTANCE}.{methodSymbol.Name}({string.Join(", ", methodSymbol.Parameters.Select(p => p.Name))});");
            }
        }

    }

    #endregion

    private static CodeFile CreateMinimalCodeFile(ApiContext context, out string endPointsName)
    {
        var methods = context.Methods;
        var ns = NamespaceBuilder.Default.Namespace(context.NameSpace);
        var endPointClass = CreateEndPointExtensionClass(context)
            .Modifiers("public static ");
        List<Node> members = [];
        var methodName = $"Map{context.FileName}EndPoints";
        endPointsName = $"global::{ns.Namespace}.{endPointClass.Name}.{methodName}";
        var extensionMethod = MethodBuilder.Default.MethodName(methodName)
            .Modifiers("public static")
            .AddParameter("this WebApplication app");
        var extensionMethodBody = new List<Statement>
        {
            $"""var group = app.MapGroup("api/{context.RouteUrl ?? context.FileName}")"""
        };
        if (context.AuthorizeInfo?.RequiredAuthorize == true)
        {
            extensionMethodBody.Add("group.RequireAuthorization();");
        }
        members.Add(extensionMethod);
        foreach (var ami in methods)
        {

            var methodSyntax = BuildPointDelegate(context, ami);
            members.Add(methodSyntax);
            var allowAnonymous = ami.MethodAuthorize.AllowAnonymous;
            var methodAuth = ami.MethodAuthorize.RequiredAuthorize;
            var apiAuth = context.AuthorizeInfo?.RequiredAuthorize ?? false;
            var auth = allowAnonymous ? ".AllowAnonymous()" : ((methodAuth || apiAuth) && !allowAnonymous ? ".RequireAuthorization()" : "");
            extensionMethodBody.Add($"""group.Map{ami.HttpMethod}("{ami.RouteUrl}", {methodSyntax.Name}){auth}""");
        }
        extensionMethod.AddBody([.. extensionMethodBody]);

        var file = CodeFile.New($"{context.InterfaceType.FormatFileName()}EndPointsExtensions.g.cs")
            .AddUsings("using Microsoft.AspNetCore.Builder;")
             .AddMembers(ns.AddMembers(endPointClass.AddMembers([.. members])));
        //.AddUsings(source.GetTargetUsings());

        return file;

        static ClassBuilder CreateEndPointExtensionClass(ApiContext apiContext)
        {
            //var additionalAttribute = source.TargetSymbol.GetAttributeInitInfo<ControllerGenerator>();
            return ClassBuilder.Default
                .ClassName($"{FormatClassName(apiContext.FileName)}EndPointsExtensions")
                .Modifiers("public")
                .AddGeneratedCodeAttribute(typeof(ControllerGenerator));
        }

        static MethodBuilder BuildPointDelegate(ApiContext api, ApiMethodInfo method)
        {
            /*
             * [global::Microsoft.AspNetCore.Mvc.{httpMethod}("...")]
             * [global::System.CodeDom.Compiler.GeneratedCode("...", "...")]
             * public <RETURN_TYPE> <METHOD_NAME>(<Parameter> p) => _proxyService_gen.<METHOD_NAME>(p);
             */
            string methodRoute = method.RouteUrl;
            string httpMethod = method.HttpMethod;
            var allowAnonymous = method.MethodAuthorize.AllowAnonymous;
            var methodAuth = method.MethodAuthorize.RequiredAuthorize;
            var apiAuth = api.AuthorizeInfo.RequiredAuthorize;
            var apiAuthScheme = api.AuthorizeInfo.AuthorizeScheme;
            var methodSymbol = method.Symbol;
            var authorizeAttribute = string.IsNullOrEmpty(apiAuthScheme)
                ? "global::Microsoft.AspNetCore.Authorization.Authorize"
                : $"""global::Microsoft.AspNetCore.Authorization.Authorize(AuthenticationSchemes = "{apiAuthScheme}")""";
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
                    .Modifiers("private static")
                    .MethodName(methodSymbol.Name)
                    .Async(IsTask)
                    .ReturnType(methodReturn)
                    .AttributeIf(allowAnonymous, "global::Microsoft.AspNetCore.Authorization.AllowAnonymous")
                    .AttributeIf((methodAuth || apiAuth) && !allowAnonymous, authorizeAttribute)
                    .AddGeneratedCodeAttribute(typeof(ControllerGenerator))
                    .AddParameter(GenerateParameter(httpMethod, methodSymbol))
                    .AddParameter($"{api.TypeName} {PROXY_INSTANCE}")
                    .AddBody(methodReturnResult, finalReturn, finalJsonReturn);
            }
            else
            {
                return MethodBuilder.Default
                    .Modifiers("private static")
                    .MethodName(methodSymbol.Name)
                    .ReturnType(methodSymbol.ReturnType.ToDisplayString())
                    .AttributeIf(allowAnonymous, "global::Microsoft.AspNetCore.Authorization.AllowAnonymous")
                    .AttributeIf((methodAuth || apiAuth) && !allowAnonymous, authorizeAttribute)
                    .AddGeneratedCodeAttribute(typeof(ControllerGenerator))
                    .AddParameter(GenerateParameter(httpMethod, methodSymbol))
                    .AddParameter($"{api.TypeName} {PROXY_INSTANCE}")
                    .Lambda(
                        $"{PROXY_INSTANCE}.{methodSymbol.Name}({string.Join(", ", methodSymbol.Parameters.Select(p => p.Name))});");
            }
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
        return [.. enumerable];

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

    private static string TryGetHttpMethod(AttributeData? data)
    {
        if (data != null)
        {
            if (data.GetNamedValue("Method", out var m))
            {
                return WebMethod[(int)m!];
            }
        }
        return WebMethod[1];
    }
}