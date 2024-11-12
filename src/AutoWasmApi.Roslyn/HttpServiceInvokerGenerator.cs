using Generators.Shared;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;
using static AutoWasmApiGenerator.GeneratorHelpers;
using Generators.Shared.Builder;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection.Metadata;
using System.Text.RegularExpressions;

namespace AutoWasmApiGenerator
{
    [Generator(LanguageNames.CSharp)]
    public class HttpServiceInvokerGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            //var list = context.SyntaxProvider.ForAttributeWithMetadataName(
            //   ApiInvokerAssemblyAttributeFullName,
            //   static (node, token) => true,
            //   static (c, t) => c);

            context.RegisterSourceOutput(context.CompilationProvider, static (context, compilation) =>
            {
                try
                {
                    if (!compilation.Assembly.HasAttribute(ApiInvokerAssemblyAttributeFullName))
                    {
                        return;
                    }
                    var all = compilation.GetAllSymbols(ApiInvokerAttributeFullName);
                    foreach (var item in all)
                    {
                        if (!item.HasAttribute(WebControllerAttributeFullName))
                        {
                            continue;
                        }
                        if (CreateCodeFile(item, context, out var file))
                        {
#if DEBUG
                            var ss = file.ToString();
#endif
                            context.AddSource(file);
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor(
                        id: "ERROR00001",
                        title: "生成错误",
                        messageFormat: ex.Message,
                        category: typeof(HttpServiceInvokerGenerator).FullName!,
                        defaultSeverity: DiagnosticSeverity.Warning,
                        isEnabledByDefault: true
                        ), Location.None));
                }
            });
        }

        private static bool CreateCodeFile(INamedTypeSymbol interfaceSymbol, SourceProductionContext context, [NotNullWhen(true)] out CodeFile? file)
        {
            var methods = interfaceSymbol.GetAllMethodWithAttribute(WebMethodAttributeFullName);
            List<Node> members = new List<Node>();
            _ = interfaceSymbol.GetAttribute(WebControllerAttributeFullName, out var controllerAttrData);
            var ns = NamespaceBuilder.Default.Namespace(interfaceSymbol.ContainingNamespace.ToDisplayString());
            var invokeClass = CreateHttpClassBuilder(interfaceSymbol);
            var scopeName = interfaceSymbol.FormatClassName();
            var route = controllerAttrData.GetNamedValue("Route") as string;
            bool needAuth = (bool)(controllerAttrData.GetNamedValue("Authorize") ?? false);
            foreach (var method in methods)
            {
                var result = BuildMethod(method, route, scopeName, needAuth, out var n);
                if (n && !needAuth)
                {
                    needAuth = true;
                }
                if (result.Item2 != null)
                {
                    context.ReportDiagnostic(result.Item2);
                    file = null;
                    return false;
                }
                members.Add(result.Item1!);
            }

            var fields = BuildField(needAuth);
            var constructor = BuildConstructor(interfaceSymbol, needAuth);
            members.AddRange(fields);
            members.Add(constructor);

            file = CodeFile.New($"{interfaceSymbol.FormatFileName()}ApiInvoker.g.cs")
                .AddUsings("using Microsoft.Extensions.DependencyInjection;")
               .AddMembers(ns.AddMembers(invokeClass.AddMembers([.. members])));

            return true;
        }

        private static (MethodBuilder?, Diagnostic?) BuildMethod((IMethodSymbol, AttributeData?) method, string? route, string scopeName, bool controllerAuth, out bool needAuth)
        {
            var methodSymbol = method.Item1;
            var methodAttribute = method.Item2;

            var allowsAnonymous = (bool)(methodAttribute.GetNamedValue("AllowAnonymous") ?? false);
            var authorize = (bool)(methodAttribute.GetNamedValue("Authorize") ?? false);
            needAuth = !allowsAnonymous && (authorize || controllerAuth);

            // 检查当前返回类型是否是Task或Task<T>
            // 如果检查类型不符合要求，说明不是异步方法
            // 返回错误信息
            var returnTypeInfo = methodSymbol.ReturnType;
            var isTask = returnTypeInfo.Name == "Task";
            var isGenericTask = returnTypeInfo.OriginalDefinition.ToDisplayString() == "System.Threading.Tasks.Task<T>";

            if (!isTask && !isGenericTask)
            {
                return (null, DiagnosticDefinitions.WAG00005(methodSymbol.Locations.FirstOrDefault()));
            }

            if (methodSymbol.HasAttribute(NotSupported))
            {
                var b = MethodBuilder.Default
                        .MethodName(methodSymbol.Name)
                        .Generic([.. methodSymbol.GetTypeParameters()])
                        .ReturnType(methodSymbol.ReturnType.ToDisplayString())
                        .AddParameter([.. methodSymbol.Parameters.Select(p => $"{p.Type.ToDisplayString()} {p.Name}")])
                        .AddGeneratedCodeAttribute(typeof(HttpServiceInvokerGenerator))
                        .Lambda("throw new global::System.NotSupportedException()");
                return (b, null);
            }

            // 检查当前返回类型是否是Task或Task<T>
            // 如果检查类型不符合要求，说明不是异步方法
            // 返回错误信息
            var returnTypeInfo = methodSymbol.ReturnType;
            var isTask = returnTypeInfo.Name == "Task";
            var isGenericTask = returnTypeInfo.OriginalDefinition.ToDisplayString() == "System.Threading.Tasks.Task<TResult>";

            if (!isTask && !isGenericTask)
            {
                return (null, DiagnosticDefinitions.WAG00005(methodSymbol.Locations.FirstOrDefault()));
            }

            string webMethod;
            if (!methodAttribute.GetNamedValue("Method", out var v))
            {
                webMethod = "Post";
            }
            else
            {
                webMethod = WebMethod[(int)v!];
            }
            var methodScoped = methodSymbol.Name.Replace("Async", "");
            var customRoute = methodAttribute?.GetNamedValue("Route")?.ToString();
            string methodRoute;
            bool useRouteParam = false;
            if (string.IsNullOrEmpty(customRoute))
            {
                methodRoute = methodScoped;
            }
            else if (Regex.Match(customRoute, "{.+}").Success)
            {
                useRouteParam = true;
                methodRoute = $"{methodScoped}/{customRoute}";
            }
            else
            {
                methodRoute = customRoute!;
            }
            //var methodRoute = $"{methodAttribute?.GetNamedValue("Route") ?? methodSymbol.Name.Replace("Async", "")}";
            List<Statement> statements =
            [
                // var url = "";
                // var client = clientFactory.CreateClient(nameof(<TYPE>));
                $"var _client_gen = this.clientFactory.CreateClient(\"{scopeName}\")",
                // var request = new HttpRequestMessage();
                "var _request_gen = new global::System.Net.Http.HttpRequestMessage()",
                // request.Method = HttpMethod.<Method>
                $"_request_gen.Method = global::System.Net.Http.HttpMethod.{webMethod}",
            ];
            if (needAuth)
            {
                statements.Add("await headerHandler.SetRequestHeaderAsync(_request_gen)");
            }

            // 处理参数标签 
            var paramInfos = methodSymbol.Parameters.Select(p =>
            {
                if (p.GetAttribute(WebMethodParameterBindingAttribute, out var ad))
                {
                    ad!.GetConstructorValue(0, out var bindingType);
                    var t = (int)bindingType!;
                    return ((int)bindingType!, p);
                }
                if (useRouteParam && customRoute!.Contains(p.Name))
                {
                    return (1, p);
                }
                return (-1, p);
            });

            // 0 - Query
            // 1 - Route
            // 2 - Form
            // 3 - Body
            // 4 - Header

            #region 检查参数配置

            var routerParameters = paramInfos.Where(t => t.Item1 == 1);
            foreach ((int _, IParameterSymbol p) item in routerParameters)
            {
                if (!methodRoute.Contains($"{{{item.p.Name}}}"))
                {
                    return (null, DiagnosticDefinitions.WAG00006(methodSymbol.Locations.FirstOrDefault(), methodSymbol.ToDisplayString()));
                }
            }

            if (paramInfos.Any(t => t.Item1 == 2) && paramInfos.Any(t => t.Item1 == 3))
            {
                return (null, DiagnosticDefinitions.WAG00007(methodSymbol.Locations.FirstOrDefault(), methodSymbol.ToDisplayString()));
            }
            #endregion

            var url = $"""
                var _url_gen = $"api/{route ?? scopeName}/{methodRoute}"
                """;
            statements.Add(url);

            #region 处理Query参数

            var queryParameters = paramInfos.Where(t => (t.Item1 == -1 && webMethod == "Get") || t.Item1 == 0);

            if (queryParameters.Any())
            {
                statements.Add("var _queries_gen = new List<string>()");
                foreach (var item in queryParameters)
                {
                    var p = item.p;
                    if (p.Type is INamedTypeSymbol { TypeKind: TypeKind.Class, SpecialType: not SpecialType.System_String } parameterClassType)
                    {
                        var properties = parameterClassType.GetMembers().Where(m => m.Kind == SymbolKind.Property);
                        foreach (var prop in properties)
                        {
                            statements.Add($$"""_queries_gen.Add($"{nameof({{p.Name}}.{{prop.Name}})}={{{p.Name}}.{{prop.Name}}}")""");
                        }
                    }
                    else
                    {
                        statements.Add($$"""_queries_gen.Add($"{nameof({{p.Name}})}={{{p.Name}}}")""");
                    }
                }
                var setUrl = """
                        _url_gen = $"{_url_gen}?{string.Join("&", _queries_gen)}"
                        """;
                statements.Add(setUrl);
            }

            #endregion

            #region 处理Form参数

            var formParameters = paramInfos.Where(t => t.Item1 == 2);

            if (formParameters.Any())
            {
                statements.Add("var _formDatas_gen = new List<global::System.Collections.Generic.KeyValuePair<string, string>>()");
                foreach (var item in formParameters)
                {
                    var p = item.p;
                    if (p.Type is INamedTypeSymbol { TypeKind: TypeKind.Class, SpecialType: not SpecialType.System_String } parameterClassType)
                    {
                        var properties = parameterClassType.GetMembers().Where(m => m.Kind == SymbolKind.Property);
                        foreach (var prop in properties)
                        {
                            statements.Add($$"""
                                _formDatas_gen.Add(new global::System.Collections.Generic.KeyValuePair<string, string>(nameof({{p.Name}}.{{prop.Name}}), $"{{{p.Name}}.{{prop.Name}}}"))
                                """);
                        }
                    }
                    else
                    {
                        statements.Add($$"""_formDatas_gen.Add(new global::System.Collections.Generic.KeyValuePair<string, string>(nameof({{p.Name}}), $"{{{p.Name}}}"))""");
                    }
                }
                statements.Add("var _formContent_gen = new global::System.Net.Http.FormUrlEncodedContent(_formDatas_gen)");
                statements.Add("_formContent_gen.Headers.ContentType = new(\"application/x-www-form-urlencoded\")");
                statements.Add("""_request_gen.Content = _formContent_gen""");

            }

            #endregion

            #region 处理Body参数

            var bodyParameters = paramInfos.Where(t => (t.Item1 == -1 && webMethod != "Get") || t.Item1 == 3);
            if (bodyParameters.Count() > 1)
            {
                return (null, DiagnosticDefinitions.WAG00008(methodSymbol.Locations.FirstOrDefault(), methodSymbol.ToDisplayString()));
            }

            if (bodyParameters.Any())
            {
                var p = bodyParameters.First().p;
                statements.Add($"var _json_gen = global::System.Text.Json.JsonSerializer.Serialize({p.Name})");
                statements.Add("""_request_gen.Content = new global::System.Net.Http.StringContent(_json_gen, global::System.Text.Encoding.Default, "application/json")""");
            }

            #endregion

            #region 处理Header参数

            var headerParameters = paramInfos.Where(t => t.Item1 == 4);

            if (headerParameters.Any())
            {
                foreach (var item in headerParameters)
                {
                    var p = item.p;
                    if (p.Type is INamedTypeSymbol { TypeKind: TypeKind.Class, SpecialType: not SpecialType.System_String } parameterClassType)
                    {
                        var properties = parameterClassType.GetMembers().Where(m => m.Kind == SymbolKind.Property);
                        foreach (var prop in properties)
                        {
                            statements.Add($$"""_request_gen.Headers.Add(nameof({{p.Name}}.{{prop.Name}}), $"{{{p.Name}}.{{prop.Name}}}")""");
                        }
                    }
                    else
                    {
                        statements.Add($$"""_request_gen.Headers.Add(nameof({{p.Name}}), $"{{{p.Name}}}")""");
                    }
                }
            }

            #endregion

            statements.Add("_request_gen.RequestUri = new global::System.Uri(_url_gen, UriKind.Relative)");
            var returnType = methodSymbol.ReturnType.GetGenericTypes().FirstOrDefault() ?? methodSymbol.ReturnType;

            if (methodSymbol.ReturnsVoid || returnType.ToDisplayString() == "System.Threading.Tasks.Task")
            {
                statements.Add("_ = await _client_gen.SendAsync(_request_gen)");
            }
            else
            {
                statements.Add("var _response_gen = await _client_gen.SendAsync(_request_gen)");
                statements.Add("_response_gen.EnsureSuccessStatusCode()");
                // 返回值是复杂类型，使用Json反序列化
                if (returnType is { TypeKind: TypeKind.Class, SpecialType: not SpecialType.System_String })
                {
                    statements.Add("var _stream_gen = await _response_gen.Content.ReadAsStreamAsync()");
                    //return System.Text.Json.JsonSerializer.Deserialize<RETURN_TYPE>(jsonStream, jsonOptions);
                    statements.Add($"return global::System.Text.Json.JsonSerializer.Deserialize<{returnType.ToDisplayString()}>(_stream_gen, _JSON_OPTIONS_gen);");
                }
                else
                {
                    statements.Add("var _str_gen = await _response_gen.Content.ReadAsStringAsync()");
                    if (returnType.SpecialType == SpecialType.System_String)
                    {
                        statements.Add("return _str_gen");
                    }
                    else if (returnType.IsValueType)
                    {
                        statements.Add($"{returnType.ToDisplayString()}.TryParse(_str_gen, out var val)");
                        statements.Add("return val");
                    }
                    else
                    {
                        return (null, DiagnosticDefinitions.WAG00009(methodSymbol.Locations.FirstOrDefault()));
                    }
                }
            }

            var builder = MethodBuilder.Default
                .MethodName(methodSymbol.Name)
                .Generic([.. methodSymbol.GetTypeParameters()])
                .Async()
                .ReturnType(methodSymbol.ReturnType.ToDisplayString())
                .AddParameter([.. methodSymbol.Parameters.Select(p => $"{p.Type.ToDisplayString()} {p.Name}")])
                .AddGeneratedCodeAttribute(typeof(HttpServiceInvokerGenerator))
                .AddBody([.. statements]);
            return (builder, null);
        }

        private static IEnumerable<FieldBuilder> BuildField(bool needAuth)
        {
            // private readonly JsonSerializerOptions jsonOptions;
            yield return FieldBuilder.Default.MemberType("global::System.Text.Json.JsonSerializerOptions")
                .FieldName("_JSON_OPTIONS_gen");
            // private readonly IHttpClientFactory clientFactory;
            yield return FieldBuilder.Default
                .MemberType("global::System.Net.Http.IHttpClientFactory")
                .FieldName("clientFactory");
            if (needAuth)
            {
                yield return FieldBuilder.Default
                    .MemberType("global::AutoWasmApiGenerator.IHttpClientHeaderHandler")
                    .FieldName("headerHandler");
            }
        }

        private static ConstructorBuilder BuildConstructor(INamedTypeSymbol classSymbol, bool needAuth)
        {
            List<string> parameters = [
                "global::System.Net.Http.IHttpClientFactory factory"
                ];
            List<Statement> body = ["clientFactory = factory;"];
            if (needAuth)
            {
                //parameters.Add("global::AutoWasmApiGenerator.IHttpClientHeaderHandler handler");
                parameters.Add("global::System.IServiceProvider services");
                body.Add("headerHandler = services.GetService<global::AutoWasmApiGenerator.IHttpClientHeaderHandler>() ?? global::AutoWasmApiGenerator.DefaultHttpClientHeaderHandler.Default");
            }

            return ConstructorBuilder.Default
                .MethodName($"{FormatClassName(classSymbol.MetadataName)}ApiInvoker")
                .AddParameter([.. parameters])
                .AddBody([.. body])
                .AddBody("_JSON_OPTIONS_gen = new global::System.Text.Json.JsonSerializerOptions() { PropertyNameCaseInsensitive = true };");
        }

        private static ClassBuilder CreateHttpClassBuilder(INamedTypeSymbol interfaceSymbol)
        {
            IEnumerable<string> additionalAttribute = [];
            if (interfaceSymbol.GetAttribute(ApiInvokerAttributeFullName, out var data))
            {
                //var o = data.GetAttributeValue(nameof(ApiInvokerGeneraAttribute.Attribute));
                additionalAttribute = interfaceSymbol.GetAttributeInitInfo(ApiInvokerAttributeFullName, data!);
            }


            return ClassBuilder.Default
                .ClassName($"{FormatClassName(interfaceSymbol.MetadataName)}ApiInvoker")
                .AddGeneratedCodeAttribute(typeof(HttpServiceInvokerGenerator))
                .Attribute([.. additionalAttribute.Select(i => i.ToString())])
                .BaseType(interfaceSymbol.ToDisplayString());
        }
    }
}
