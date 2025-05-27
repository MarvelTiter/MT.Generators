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
using System.Text;
using System;

namespace AutoWasmApiGenerator
{
    [Generator(LanguageNames.CSharp)]
    public class ApiInvokerGenerator : IIncrementalGenerator
    {
        private const string CUSTOM_JSON_OPTION = "AutoWasmApiGenerator.AutoWasmApiGeneratorJsonHelper.Option";

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
                    _ = compilation.Assembly.GetAttribute(ApiInvokerAssemblyAttributeFullName, out var asmAttributeData);
                    var all = compilation.GetAllSymbols(WebControllerAttributeFullName);
                    foreach (var item in all)
                    {
                        if (item.HasAttribute(ApiNotSupported))
                            continue;
                        if (!CreateCodeFile(item, asmAttributeData!, context, out var file))
                            continue;
#if DEBUG
                        var ss = file.ToString();
#endif
                        context.AddSource(file);
                    }
                }
                catch (System.Exception ex)
                {
                    context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor(
                        id: "ERROR00001",
                        title: "生成错误",
                        messageFormat: ex.Message,
                        category: typeof(ApiInvokerGenerator).FullName!,
                        defaultSeverity: DiagnosticSeverity.Warning,
                        isEnabledByDefault: true
                    ), Location.None));
                }
            });
        }

        private static bool CreateCodeFile(INamedTypeSymbol interfaceSymbol
            , AttributeData returnConfig
            , SourceProductionContext context
            , [NotNullWhen(true)] out CodeFile? file)
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
                var result = BuildMethod(interfaceSymbol, method, returnConfig, route, scopeName, needAuth, out var n);
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

            var fields = BuildField();
            var constructor = BuildConstructor(interfaceSymbol);
            members.AddRange(fields);
            members.Add(constructor);

            file = CodeFile.New($"{interfaceSymbol.FormatFileName()}ApiInvoker.g.cs")
                .AddUsings("using Microsoft.Extensions.DependencyInjection;")
                .AddMembers(ns.AddMembers(invokeClass.AddMembers([.. members])));

            return true;
        }

        private static (MethodBuilder?, Diagnostic?) BuildMethod(INamedTypeSymbol iSymbol
            , (IMethodSymbol, AttributeData?) method
            , AttributeData returnConfig
            , string? route
            , string scopeName
            , bool controllerAuth
            , out bool needAuth)
        {
            var methodSymbol = method.Item1;
            var methodAttribute = method.Item2;

            var allowsAnonymous = (bool)(methodAttribute.GetNamedValue("AllowAnonymous") ?? false);
            var authorize = (bool)(methodAttribute.GetNamedValue("Authorize") ?? false);
            needAuth = !allowsAnonymous && (authorize || controllerAuth);

            if (methodSymbol.HasAttribute(ApiNotSupported))
            {
                var b = MethodBuilder.Default
                    .MethodName(methodSymbol.Name)
                    .Generic([.. methodSymbol.GetTypeParameters()])
                    .ReturnType(methodSymbol.ReturnType.ToDisplayString())
                    .AddParameter([.. methodSymbol.Parameters.Select(p => $"{p.Type.ToDisplayString()} {p.Name}")])
                    .AddGeneratedCodeAttribute(typeof(ApiInvokerGenerator))
                    .Lambda("throw new global::System.NotSupportedException()");
                return (b, null);
            }

            // 检查当前返回类型是否是Task或Task<T>
            // 如果检查类型不符合要求，说明不是异步方法
            // 返回错误信息
            //var returnTypeInfo = methodSymbol.ReturnType;
            //var isTask = returnTypeInfo.Name == "Task";
            //var isGenericTask = returnTypeInfo.OriginalDefinition.ToDisplayString() == "System.Threading.Tasks.Task<TResult>";
            var returnInfo = methodSymbol.GetReturnTypeInfo();
            if (!returnInfo.IsTask)
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

                if (p.Type is { TypeKind: TypeKind.Class, SpecialType: not SpecialType.System_String })
                {
                    return (3, p);
                }

                return (-1, p);
            }).ToArray();

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

            var queryParameters = paramInfos.Where(t => t.Item1 is -1 or 0).ToArray();

            if (queryParameters.Length > 0)
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
                            if (prop.IsStatic) continue;
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

            var formParameters = paramInfos.Where(t => t.Item1 == 2).ToArray();

            if (formParameters.Length > 0)
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
                statements.Add("""_formContent_gen.Headers.ContentType = new("application/x-www-form-urlencoded")""");
                statements.Add("_request_gen.Content = _formContent_gen");
            }

            #endregion

            #region 处理Body参数

            var bodyParameters = paramInfos.Where(t => t.Item1 == 3).ToArray();
            if (bodyParameters.Length > 1)
            {
                return (null, DiagnosticDefinitions.WAG00008(methodSymbol.Locations.FirstOrDefault(), methodSymbol.ToDisplayString()));
            }

            if (bodyParameters.Length > 0)
            {
                var p = bodyParameters.First().p;
                statements.Add($"var _json_gen = global::System.Text.Json.JsonSerializer.Serialize({p.Name})");
                statements.Add("""_request_gen.Content = new global::System.Net.Http.StringContent(_json_gen, global::System.Text.Encoding.UTF8, "application/json")""");
            }

            #endregion

            #region 处理Header参数

            var headerParameters = paramInfos.Where(t => t.Item1 == 4).ToArray();

            if (headerParameters.Length > 0)
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
            //var returnType = methodSymbol.ReturnType.GetGenericTypes().FirstOrDefault() ?? methodSymbol.ReturnType;
            var returnType = returnInfo.ReturnType;
            var tcf = TryCatch.Default;
            statements.Add($"var _send_context = new global::AutoWasmApiGenerator.SendContext(typeof({iSymbol.ToDisplayString()}), nameof({methodSymbol.Name}), _request_gen)");
            if (paramInfos.Length > 0)
            {
                var ps = paramInfos.Select(p => p.p.Name);
                statements.Add($"_send_context.Parameters = [{string.Join(", ", ps)}]");
            }
            if (returnInfo.HasReturn)
            {
                statements.Add($"_send_context.ReturnType = typeof({returnInfo.ReturnType.ToDisplayString(NullableFlowState.None)})");
            }
            tcf.AddBody("await delegatingHandler.BeforeSendAsync(_send_context)");
            tcf.AddBody("using var _response_gen = await _client_gen.SendAsync(_request_gen)");
            tcf.AddBody("_send_context.Response = _response_gen");
            tcf.AddBody("_response_gen.EnsureSuccessStatusCode()");
            tcf.AddBody("await delegatingHandler.AfterSendAsync(_send_context)");
            if (returnInfo.HasReturn)
            {
                //statements.Add("using var _response_gen = await _client_gen.SendAsync(_request_gen)");
                // 返回值是复杂类型，使用Json反序列化
                if (returnType is { TypeKind: TypeKind.Class, SpecialType: not SpecialType.System_String, IsTupleType: false })
                {
                    tcf.AddBody("using var _stream_gen = await _response_gen.Content.ReadAsStreamAsync()");
                    //return System.Text.Json.JsonSerializer.Deserialize<RETURN_TYPE>(jsonStream, jsonOptions);
                    tcf.AddBody($"return global::System.Text.Json.JsonSerializer.Deserialize<{returnType.ToDisplayString()}>(_stream_gen, {CUSTOM_JSON_OPTION});");
                }
                else if (returnType.IsTupleType)
                {
                    tcf.AddBody("var _json_string_gen = await _response_gen.Content.ReadAsStringAsync()");
                    //return System.Text.Json.JsonSerializer.Deserialize<RETURN_TYPE>(jsonStream, jsonOptions);
                    tcf.AddBody($"var _jsonElement_gen = global::System.Text.Json.JsonDocument.Parse(_json_string_gen).RootElement;");
                    var tupleObject = new StringBuilder();
                    ConvertJsonElementToTuple(tupleObject, (INamedTypeSymbol)returnType, "_jsonElement_gen");
                    tcf.AddBody($"return {tupleObject}");
                }
                else
                {
                    tcf.AddBody("var _str_gen = await _response_gen.Content.ReadAsStringAsync()");
                    if (returnType.SpecialType == SpecialType.System_String)
                    {
                        tcf.AddBody("return _str_gen");
                    }
                    else if (returnType.IsValueType && returnType.SpecialType != SpecialType.None)
                    {
                        tcf.AddBody($"{returnType.ToDisplayString()}.TryParse(_str_gen, out var val)");
                        tcf.AddBody("return val");
                    }
                    else
                    {
                        return (null, DiagnosticDefinitions.WAG00009(methodSymbol.TryGetLocation()));
                    }
                }
            }
            tcf.AddCatch(c =>
            {
                c.Exception = "Exception ex";
                c.AddBody("var _ex_context = new global::AutoWasmApiGenerator.ExceptionContext(_send_context, ex)");
                c.AddBody("await delegatingHandler.OnExceptionAsync(_ex_context)");
                c.AddBody(IfStatement.Default.If("!_ex_context.Handled").AddStatement("throw"));
                if (returnInfo.HasReturn)
                {
                    // 尝试从IExceptionResultFactory获取返回值
                    c.AddBody("var errorResultFactory = serviceProvider.GetService<global::AutoWasmApiGenerator.IExceptionResultFactory>()");
                    c.AddBody(IfStatement.Default.If($"errorResultFactory?.GetErrorResult<{returnInfo.ReturnType.ToDisplayString()}>(_ex_context, out var _default_result) == true").AddStatement("return _default_result"));
                    // 尝试从约束属性构建返回值 -> 无参构造函数，有类似success命名的布尔值属性，有类似message、msg命名的字符串属性
                    // 自定义类型
                    string[] successFlags;
                    string[] messageFlags;
                    if (returnConfig.GetNamedValue<string>("SuccessFlag", out var successProp))
                    {
                        successFlags = successProp!.Split([',', '，', ' ', '|'], StringSplitOptions.RemoveEmptyEntries);
                    }
                    else
                    {
                        successFlags = ["success"];
                    }
                    if (returnConfig.GetNamedValue<string>("MessageFlag", out var messageProp))
                    {
                        messageFlags = messageProp!.Split([',', '，', ' ', '|'], StringSplitOptions.RemoveEmptyEntries);
                    }
                    else
                    {
                        messageFlags = ["message", "msg"];
                    }
                    if (returnType is { TypeKind: TypeKind.Class, SpecialType: not SpecialType.System_String, IsTupleType: false })
                    {
                        // 无参构造函数
                        var hasNoParamCtor = returnType.GetMembers().Where(m => m.Kind == SymbolKind.Method).Cast<IMethodSymbol>().FirstOrDefault(m => m.MethodKind == MethodKind.Constructor && m.Parameters.Length == 0) is not null;
                        if (hasNoParamCtor)
                        {
                            var success = returnType.GetAllMembers(_ => true).FirstOrDefault(m => m.Kind == SymbolKind.Property && FindMatchProperty(successFlags, m.Name)) as IPropertySymbol;
                            var message = returnType.GetAllMembers(_ => true).FirstOrDefault(m => m.Kind == SymbolKind.Property && FindMatchProperty(messageFlags, m.Name)) as IPropertySymbol;
                            if (success is not null || message is not null)
                            {
                                c.AddBody($"{returnType.ToDisplayString()} _gen_return = new()");
                                if (success is not null && success.Type.SpecialType == SpecialType.System_Boolean)
                                {
                                    c.AddBody($"_gen_return.{success.Name} = false");
                                }
                                if (message is not null && message.Type.SpecialType == SpecialType.System_String)
                                {
                                    c.AddBody($"_gen_return.{message.Name} = ex.Message");
                                }
                                c.AddBody("return _gen_return");
                            }
                        }

                    }
                    else if (returnType is INamedTypeSymbol { IsTupleType: true } t)
                    {
                        var elements = t.TupleElements;
                        c.AddBody($"return ({string.Join(", ", elements.Select(CheckFieldNameAndType))})");
                        string CheckFieldNameAndType(IFieldSymbol field)
                        {
                            if (FindMatchProperty(successFlags, field.Name) && field.Type.SpecialType == SpecialType.System_Boolean)
                            {
                                return "false";
                            }
                            if (FindMatchProperty(messageFlags, field.Name) && field.Type.SpecialType == SpecialType.System_String)
                            {
                                return "ex.Message";
                            }
                            else
                            {
                                return "default";
                            }
                        }
                    }
                    else
                    {
                        c.AddBody("return default");
                    }
                }
            });
            statements.Add(tcf);
            var builder = MethodBuilder.Default
                .MethodName(methodSymbol.Name)
                .Generic([.. methodSymbol.GetTypeParameters()])
                .Async()
                .ReturnType(methodSymbol.ReturnType.ToDisplayString())
                .AddParameter([.. methodSymbol.Parameters.Select(p => $"{p.Type.ToDisplayString()} {p.Name}")])
                .AddGeneratedCodeAttribute(typeof(ApiInvokerGenerator))
                .AddBody([.. statements]);
            return (builder, null);
        }

        private static bool FindMatchProperty(string[] successFlags, string name)
        {
            var lower = name.ToLowerInvariant();
            foreach (var item in successFlags)
            {
                var lowerItem = item.ToLowerInvariant();
                if (lower.Contains(lowerItem) || lowerItem.Contains(lower))
                    return true;
            }
            return false;
        }

        private static void ConvertJsonElementToTuple(StringBuilder tupleObject, INamedTypeSymbol tupleType, string jsonElement)
        {
            tupleObject.Append('(');
            bool shouldRemoveLast = false;
            foreach (var field in tupleType.TupleElements)
            {
                var prop = field.Name;// ConvertCamelCaseName(field.Name);
                var jsonValue = $"""{jsonElement}.GetProperty("{prop}")""";
                if (field.Type.IsTupleType)
                {
                    ConvertJsonElementToTuple(tupleObject, (INamedTypeSymbol)field.Type, jsonValue);
                    shouldRemoveLast = false;
                }
                else
                {
                    if (GetJsonAccessor(field.Type, out var accessor))
                    {
                        tupleObject.Append($"{jsonValue}.{accessor},");
                        shouldRemoveLast = true;
                    }
                }
            }
            if (shouldRemoveLast)
                tupleObject.Remove(tupleObject.Length - 1, 1);
            tupleObject.Append(')');

            static bool GetJsonAccessor(ITypeSymbol type, out string? accessor)
            {
                accessor = type.SpecialType switch
                {
                    SpecialType.System_String => "GetString()",
                    SpecialType.System_Enum => throw new NotImplementedException(),
                    SpecialType.System_Boolean => "GetBoolean()",
                    //SpecialType.System_Char => throw new NotImplementedException(),
                    SpecialType.System_SByte => "GetSByte()",
                    SpecialType.System_Byte => "GetByte()",
                    SpecialType.System_Int16 => "GetInt16()",
                    SpecialType.System_UInt16 => "GetUInt16()",
                    SpecialType.System_Int32 => "GetInt32()",
                    SpecialType.System_UInt32 => "GetUInt32()",
                    SpecialType.System_Int64 => "GetInt64()",
                    SpecialType.System_UInt64 => "GetUInt64()",
                    SpecialType.System_Decimal => "GetDecimal()",
                    SpecialType.System_Single => "GetSingle()",
                    SpecialType.System_Double => "GetDouble()",
                    //SpecialType.System_Array => throw new NotImplementedException(),
                    //SpecialType.System_Collections_IEnumerable => throw new NotImplementedException(),
                    //SpecialType.System_Collections_Generic_IEnumerable_T => throw new NotImplementedException(),
                    //SpecialType.System_Collections_Generic_IList_T => throw new NotImplementedException(),
                    //SpecialType.System_Collections_Generic_ICollection_T => throw new NotImplementedException(),
                    //SpecialType.System_Collections_IEnumerator => throw new NotImplementedException(),
                    //SpecialType.System_Collections_Generic_IEnumerator_T => throw new NotImplementedException(),
                    //SpecialType.System_Collections_Generic_IReadOnlyList_T => throw new NotImplementedException(),
                    //SpecialType.System_Collections_Generic_IReadOnlyCollection_T => throw new NotImplementedException(),
                    //SpecialType.System_Nullable_T => throw new NotImplementedException(),
                    SpecialType.System_DateTime => "GetDateTime()",
                    //SpecialType.System_Runtime_CompilerServices_IsVolatile => throw new NotImplementedException(),
                    //SpecialType.System_IDisposable => throw new NotImplementedException(),
                    //SpecialType.System_TypedReference => throw new NotImplementedException(),
                    //SpecialType.System_ArgIterator => throw new NotImplementedException(),
                    //SpecialType.System_RuntimeArgumentHandle => throw new NotImplementedException(),
                    //SpecialType.System_RuntimeFieldHandle => throw new NotImplementedException(),
                    //SpecialType.System_RuntimeMethodHandle => throw new NotImplementedException(),
                    //SpecialType.System_RuntimeTypeHandle => throw new NotImplementedException(),
                    //SpecialType.System_IAsyncResult => throw new NotImplementedException(),
                    //SpecialType.System_AsyncCallback => throw new NotImplementedException(),
                    //SpecialType.System_Runtime_CompilerServices_RuntimeFeature => throw new NotImplementedException(),
                    //SpecialType.System_Runtime_CompilerServices_PreserveBaseOverridesAttribute => throw new NotImplementedException(),
                    //SpecialType.System_Runtime_CompilerServices_InlineArrayAttribute => throw new NotImplementedException(),
                    //其他类型处理...
                    _ => null
                };
                return accessor != null;
            }
        }

        private static IEnumerable<FieldBuilder> BuildField()
        {
            // private readonly IHttpClientFactory clientFactory;
            yield return FieldBuilder.Default
                .MemberType("global::System.Net.Http.IHttpClientFactory")
                .FieldName("clientFactory");
            yield return FieldBuilder.Default
                .MemberType("global::AutoWasmApiGenerator.IGeneratedApiInvokeDelegatingHandler")
                .FieldName("delegatingHandler");
            yield return FieldBuilder.Default
                .MemberType("global::System.IServiceProvider")
                .FieldName("serviceProvider");
        }

        private static ConstructorBuilder BuildConstructor(INamedTypeSymbol classSymbol)
        {
            List<string> parameters =
            [
                "global::System.Net.Http.IHttpClientFactory factory"
            ];
            List<Statement> body = ["clientFactory = factory;"];

            parameters.Add("global::System.IServiceProvider services");
            body.Add("serviceProvider = services");
            body.Add("delegatingHandler = services.GetService<global::AutoWasmApiGenerator.IGeneratedApiInvokeDelegatingHandler>() ?? global::AutoWasmApiGenerator.GeneratedApiInvokeDelegatingHandler.Default");


            return ConstructorBuilder.Default
                .MethodName($"{FormatClassName(classSymbol.MetadataName)}ApiInvoker")
                .AddParameter([.. parameters])
                .AddBody([.. body]);
        }

        private static ClassBuilder CreateHttpClassBuilder(INamedTypeSymbol interfaceSymbol)
        {
            // IEnumerable<string> additionalAttribute = [];
            // if (interfaceSymbol.GetAttribute(ApiInvokerAttributeFullName, out var data))
            // {
            //     //var o = data.GetAttributeValue(nameof(ApiInvokerGeneraAttribute.Attribute));
            //     additionalAttribute = interfaceSymbol.GetAttributeInitInfo(ApiInvokerAttributeFullName, data!);
            // }

            return ClassBuilder.Default
                .ClassName($"{FormatClassName(interfaceSymbol.MetadataName)}ApiInvoker")
                .AddGeneratedCodeAttribute(typeof(ApiInvokerGenerator))
                // .Attribute([.. additionalAttribute.Select(i => i.ToString())])
                .BaseType(interfaceSymbol.ToDisplayString());
        }

        //private static string ConvertCamelCaseName(string name)
        //{
        //    if (string.IsNullOrEmpty(name) || !char.IsUpper(name[0]))
        //    {
        //        return name;
        //    }
        //    var chars = new Span<char>([.. name]);
        //    FixCasing(chars);

        //    return new string(chars.ToArray());

        //    static void FixCasing(Span<char> chars)
        //    {
        //        for (int i = 0; i < chars.Length; i++)
        //        {
        //            if (i == 1 && !char.IsUpper(chars[i]))
        //            {
        //                break;
        //            }
        //            bool hasNext = (i + 1 < chars.Length);
        //            // Stop when next char is already lowercase.
        //            if (i > 0 && hasNext && !char.IsUpper(chars[i + 1]))
        //            {
        //                // If the next char is a space, lowercase current char before exiting.
        //                if (chars[i + 1] == ' ')
        //                {
        //                    chars[i] = char.ToLowerInvariant(chars[i]);
        //                }
        //                break;
        //            }
        //            chars[i] = char.ToLowerInvariant(chars[i]);
        //        }
        //    }
        //}
    }
}