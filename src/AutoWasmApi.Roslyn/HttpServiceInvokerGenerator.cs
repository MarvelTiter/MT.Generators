using Generators.Shared;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;
using static AutoWasmApiGenerator.GeneratorHepers;
using Generators.Shared.Builder;
using System.Diagnostics;
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
                            var ss = file!.ToString();
#endif
                            context.AddSource(file!);
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

        private static bool CreateCodeFile(INamedTypeSymbol interfaceSymbol, SourceProductionContext context, out CodeFile? file)
        {
            var methods = interfaceSymbol.GetAllMethodWithAttribute(WebMethodAttributeFullName);
            List<Node> members = new List<Node>();
            _ = interfaceSymbol.GetAttribute(WebControllerAttributeFullName, out var controllerAttrData);

            
            var scopeName = interfaceSymbol.FormatClassName();
            var route = controllerAttrData.GetNamedValue("Route") as string;
            bool needAuth = (bool)(controllerAttrData.GetNamedValue("Authorize") ?? false);
            foreach (var method in methods)
            {
                var methodSyntax = BuildMethod(method, route, scopeName,needAuth,out var n);
                if (n && !needAuth)
                {
                    needAuth = true;
                }
                members.Add(methodSyntax);
            }
            
            var fields = BuildField(needAuth);
            var constructor = BuildConstructor(interfaceSymbol, needAuth);
            members.AddRange(fields);
            members.Add(constructor);
            
            file = CodeFile.New($"{interfaceSymbol.FormatFileName()}ApiInvoker.g.cs")
               .AddMembers(NamespaceBuilder.Default.Namespace(interfaceSymbol.ContainingNamespace.ToDisplayString())
                   .AddMembers(CreateHttpClassBuilder(interfaceSymbol)
                       .AddMembers([.. members])));

            return true;
        }

        private static MethodBuilder BuildMethod((IMethodSymbol, AttributeData?) method, string? route, string scopeName,bool controllerAuth, out bool needAuth)
        {
            //methodSymbol.GetAttribute<WebMethodAttribute>(out var m);
            //classSymbol.GetAttribute<WebControllerAttribute>(out var c);
            var methodSymbol = method.Item1;
            var methodAttribute = method.Item2;

            var allowsAnonymous = (bool)(methodAttribute.GetNamedValue("AllowAnonymous") ?? false);
            var authorize = (bool)(methodAttribute.GetNamedValue("Authorize") ?? false);
            needAuth = !allowsAnonymous && (authorize || controllerAuth);
            string webMethod;
            if (!methodAttribute.GetNamedValue("Method", out var v))
            {
                webMethod = "Post";
            }
            else
            {
                webMethod = WebMethod[(int)v!];
            }
            var url = $"api/{route ?? scopeName}/{methodAttribute?.GetNamedValue("Route") ?? methodSymbol.Name.Replace("Async", "")}";
            List<Statement> statements =
            [
                // var url = "";
                $"var _url_gen = \"{url}\"",
                // var client = clientFactory.CreateClient(nameof(<TYPE>));
                $"var _client_gen = this.clientFactory.CreateClient(\"{scopeName}\")",
                // var request = new HttpRequestMessage();
                "var _request_gen = new global::System.Net.Http.HttpRequestMessage()",
                // request.Method = HttpMethod.<Method>
                $"_request_gen.Method = global::System.Net.Http.HttpMethod.{webMethod}",
            ];
            if (needAuth)
            {
                statements.Add("headerHandler.SetRequestHeader(_request_gen)");
            }
            if (webMethod == "Get")
            {
                // var queries = new List<string>();
                statements.Add("var queries = new List<string>()");
                foreach (var p in methodSymbol.Parameters)
                {
                    if (p.Type is INamedTypeSymbol { TypeKind: TypeKind.Class, SpecialType: not SpecialType.System_String } parameterClassType)
                    {
                        var properties = parameterClassType.GetMembers().Where(m => m.Kind == SymbolKind.Property);
                        // queries.Add($"{<PropName>}={PropValue}");
                        foreach (var prop in properties)
                        {
                            statements.Add($$"""queries.Add($"{nameof({{p.Name}}.{{prop.Name}})}={{{p.Name}}.{{prop.Name}}}")""");
                        }
                    }
                    else
                    {
                        statements.Add($$"""queries.Add($"{nameof({{p.Name}})}={{{p.Name}}}")""");
                    }
                }
                // request.RequestUri = new Uri($"{url}?{string.Join("&", queries)}", UriKind.Relative);
                var setUrl = """
_request_gen.RequestUri = new Uri($"{_url_gen}?{string.Join("&", queries)}", UriKind.Relative)
""";
                statements.Add(setUrl);
            }
            else
            {
                var p = methodSymbol.Parameters.FirstOrDefault();
                if (p != null)
                {
                    // var jsonContent = JsonSerializer.Serialize(value);
                    // request.Content = new StringContent(jsonContent, Encoding.Default, "application/json");
                    // request.RequestUri = new Uri(url, UriKind.Relative)
                    statements.Add($"var _json_gen = global::System.Text.Json.JsonSerializer.Serialize({p.Name})");
                    statements.Add("""_request_gen.Content = new StringContent(_json_gen, global::System.Text.Encoding.Default, "application/json")""");
                }
                statements.Add("_request_gen.RequestUri = new Uri(_url_gen, UriKind.Relative)");
            }
            var returnType = methodSymbol.ReturnType.GetGenericTypes().FirstOrDefault() ?? methodSymbol.ReturnType;
            if (methodSymbol.ReturnsVoid || returnType.ToDisplayString() == "System.Threading.Tasks.Task")
            {
                statements.Add("_ = await _client_gen.SendAsync(_request_gen)");
            }
            else
            {
                statements.Add("var _response_gen = await _client_gen.SendAsync(_request_gen)");
                statements.Add("_response_gen.EnsureSuccessStatusCode()");
                statements.Add("var _stream_gen = await _response_gen.Content.ReadAsStreamAsync()");
                //return System.Text.Json.JsonSerializer.Deserialize<RETURN_TYPE>(jsonStream, jsonOptions);
                statements.Add($"return global::System.Text.Json.JsonSerializer.Deserialize<{returnType.ToDisplayString()}>(_stream_gen, _JSON_OPTIONS_gen);");
            }

            return MethodBuilder.Default
                .MethodName(methodSymbol.Name)
                .Generic([.. methodSymbol.GetTypeParameters()])
                .Async()
                .ReturnType(methodSymbol.ReturnType.ToDisplayString())
                .AddParameter([.. methodSymbol.Parameters.Select(p => $"{p.Type.ToDisplayString()} {p.Name}")])
                .AddGeneratedCodeAttribute(typeof(HttpServiceInvokerGenerator))
                .AddBody([.. statements]);

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
            List<string> parameters = ["global::System.Net.Http.IHttpClientFactory factory"];
            List<Statement> body = ["clientFactory = factory;"];
            if (needAuth)
            {
                parameters.Add("global::AutoWasmApiGenerator.IHttpClientHeaderHandler hander");
                body.Add("headerHandler = hander");
            }

            return ConstructorBuilder.Default
                .MethodName($"{FormatClassName(classSymbol.MetadataName)}ApiInvoker")
                .AddParameter([..parameters])
                .AddBody([..body])
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
