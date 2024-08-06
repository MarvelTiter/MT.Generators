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
                    var all = compilation.GetAllSymbols<INamedTypeSymbol>(ApiInvokerAttributeFullName);
                    foreach (var item in all)
                    {
                        if (!item.HasAttribute(WebControllerAttributeFullName))
                        {
                            continue;
                        }
                        if (CreateCodeFile(item, context, out var file))
                        {
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

            var clientFactoryField = BuildField();
            var constructor = BuildConstructor(interfaceSymbol);
            members.Add(FieldBuilder.Default.MemberType("global::System.Text.Json.JsonSerializerOptions").FieldName("jsonOptions"));
            members.Add(clientFactoryField);
            members.Add(constructor);

            foreach (var method in methods)
            {
                var methodSyntax = BuildMethod(method, interfaceSymbol);
                members.Add(methodSyntax);
            }
            file = CodeFile.New($"{interfaceSymbol.FormatFileName()}ApiInvoker.g.cs")
               .AddMembers(NamespaceBuilder.Default.Namespace(interfaceSymbol.ContainingNamespace.ToDisplayString())
                   .AddMembers(CreateHttpClassBuilder(interfaceSymbol, interfaceSymbol)
                       .AddMembers([.. members])));

            return true;
        }

        private static MethodBuilder BuildMethod((IMethodSymbol, AttributeData?) method, INamedTypeSymbol interfaceSymbol)
        {
            //methodSymbol.GetAttribute<WebMethodAttribute>(out var m);
            //classSymbol.GetAttribute<WebControllerAttribute>(out var c);
            var methodSymbol = method.Item1;
            var methodAttribute = method.Item2;
            interfaceSymbol.GetAttribute(WebControllerAttributeFullName, out var webController);
            string webMethod;
            if (!methodAttribute.GetNamedValue("Method", out var v))
            {
                webMethod = "Post";
            }
            else
            {
                webMethod = WebMethod[(int)v!];
            }
            var scopeName = interfaceSymbol.FormatClassName();
            var url = $"api/{webController.GetNamedValue("Route") ?? scopeName}/{methodAttribute?.GetNamedValue("Route") ?? methodSymbol.Name.Replace("Async", "")}";
            List<Statement> statements =
            [
                // var url = "";
                $"var url = \"{url}\"",
                // var client = clientFactory.CreateClient(nameof(<TYPE>));
                $"var client = clientFactory.CreateClient(\"{scopeName}\")",
                // var request = new HttpRequestMessage();
                "var request = new global::System.Net.Http.HttpRequestMessage()",
                // request.Method = HttpMethod.<Method>
                $"request.Method = global::System.Net.Http.HttpMethod.{webMethod}",
            ];

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
request.RequestUri = new Uri($"{url}?{string.Join("&", queries)}", UriKind.Relative)
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
                    statements.Add($"var jsonContent = global::System.Text.Json.JsonSerializer.Serialize({p.Name})");
                    statements.Add("""request.Content = new StringContent(jsonContent, global::System.Text.Encoding.Default, "application/json")""");
                }
                statements.Add("request.RequestUri = new Uri(url, UriKind.Relative)");
            }
            var returnType = methodSymbol.ReturnType.GetGenericTypes().FirstOrDefault() ?? methodSymbol.ReturnType;
            if (methodSymbol.ReturnsVoid || returnType.ToDisplayString() == "System.Threading.Tasks.Task")
            {
                statements.Add("_ = await client.SendAsync(request)");
            }
            else
            {
                statements.Add("var response = await client.SendAsync(request)");
                statements.Add("response.EnsureSuccessStatusCode()");
                statements.Add("var jsonStream = await response.Content.ReadAsStreamAsync()");
                //return System.Text.Json.JsonSerializer.Deserialize<RETURN_TYPE>(jsonStream, jsonOptions);
                statements.Add($"return global::System.Text.Json.JsonSerializer.Deserialize<{returnType.ToDisplayString()}>(jsonStream, jsonOptions);");
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

        private static FieldBuilder BuildField()
        {
            // private readonly IHttpClientFactory clientFactory;
            return FieldBuilder.Default
                .MemberType("global::System.Net.Http.IHttpClientFactory")
                .FieldName("clientFactory");
        }

        private static ConstructorBuilder BuildConstructor(INamedTypeSymbol classSymbol)
        {
            return ConstructorBuilder.Default
                .MethodName($"{FormatClassName(classSymbol.MetadataName)}ApiInvoker")
                .AddParameter("global::System.Net.Http.IHttpClientFactory factory")
                .AddBody("clientFactory = factory;")
                .AddBody("jsonOptions = new global::System.Text.Json.JsonSerializerOptions() { PropertyNameCaseInsensitive = true };");
        }

        private static ClassBuilder CreateHttpClassBuilder(INamedTypeSymbol classSymbol, INamedTypeSymbol interfaceSymbol)
        {
            IEnumerable<string> additionalAttribute = [];
            if (classSymbol.GetAttribute(ApiInvokerAttributeFullName, out var data))
            {
                //var o = data.GetAttributeValue(nameof(ApiInvokerGeneraAttribute.Attribute));
                additionalAttribute = classSymbol.GetAttributeInitInfo(ApiInvokerAttributeFullName, data!);
            }


            return ClassBuilder.Default
                .ClassName($"{FormatClassName(classSymbol.MetadataName)}ApiInvoker")
                .AddGeneratedCodeAttribute(typeof(HttpServiceInvokerGenerator))
                .Attribute([.. additionalAttribute.Select(i => i.ToString())])
                .BaseType(interfaceSymbol.ToDisplayString());
        }
    }
}
