using Generators.Shared;
using Generators.Shared.Builder;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using static AutoWasmApiGenerator.GeneratorHepers;
namespace AutoWasmApiGenerator
{
    [Generator(LanguageNames.CSharp)]
    public class ControllerGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var list = context.SyntaxProvider.ForAttributeWithMetadataName(
                WebControllerAttributeFullName,
                static (node, token) => node is ClassDeclarationSyntax,
                static (c, t) => c);

            context.RegisterSourceOutput(list.Combine(context.AnalyzerConfigOptionsProvider), static (context, p) =>
            {
                var (source, options) = p;
                if (options.CheckDisableGenerator(DisableWebApiGenerator))
                {
                    return;
                }

                var classSymbol = (INamedTypeSymbol)source.TargetSymbol;
                INamedTypeSymbol? interfaceSymbol = null;
                if (classSymbol.Interfaces.Length == 0)
                {
                    interfaceSymbol = classSymbol;
                }
                else if (classSymbol.Interfaces.Length == 1)
                {
                    interfaceSymbol = classSymbol.Interfaces.First();
                }
                else
                {
                    interfaceSymbol = classSymbol.Interfaces.FirstOrDefault(i => i.GetAttribute(WebControllerAttributeFullName, out _));
                }
                if (interfaceSymbol == null)
                {
                    context.ReportDiagnostic(DiagnosticDefinitions.WAG00001(source.TargetNode.GetLocation()));
                    return;
                }
                var methods = interfaceSymbol.GetAllMethodWithAttribute(WebMethodAttributeFullName, classSymbol);
                if (methods.Any(a => a.Symbol.IsGenericMethod) || classSymbol.IsGenericType)
                {
                    context.ReportDiagnostic(DiagnosticDefinitions.WAG00004(source.TargetNode.GetLocation()));
                    return;
                }
                List<Node> members = new List<Node>();
                var localField = BuildLocalField(interfaceSymbol);
                var constructor = BuildConstructor(classSymbol, interfaceSymbol);
                members.Add(localField);
                members.Add(constructor);
                foreach (var methodSymbol in methods)
                {
                    var httpMethod = TryGetHttpMethod(methodSymbol);
                    var methodSyntax = BuildMethod(methodSymbol, httpMethod);
                    if (methodSyntax != null)
                        members.Add(methodSyntax);
                }

                var file = CodeFile.New($"{classSymbol.MetadataName}Controller.g.cs")
                .AddMembers(NamespaceBuilder.Default.Namespace(source.TargetSymbol.ContainingNamespace.ToDisplayString()).AddMembers(CreateControllerClass(source).AddMembers([.. members])))
                .AddUsings(source.GetTargetUsings());
                var code = file.ToString();
                context.AddSource(file);

            });
        }

        private static MethodBuilder? BuildMethod((IMethodSymbol, AttributeData?) data, string httpMethod)
        {
            /*
             * [global::Microsoft.AspNetCore.Mvc.{httpMethod}("...")]
             * [global::System.CodeDom.Compiler.GeneratedCode("...", "...")]
             * public <RETURN_TYPE> <METHOD_NAME>(<Parameter> p) => proxyService.<METHOD_NAME>(p);
             */
            var a = data.Item2;
            var methodSymbol = data.Item1;

            var methodRouteAttribute = $"global::Microsoft.AspNetCore.Mvc.Http{httpMethod}(\"{a?.GetNamedValue("Route")?.ToString() ?? methodSymbol.Name.Replace("Async", "")}\")";

            return MethodBuilder.Default
                  .MethodName(methodSymbol.Name)
                  .ReturnType(methodSymbol.ReturnType.ToDisplayString())
                  .Attribute(methodRouteAttribute)
                  .AddGeneratedCodeAttribute(typeof(ControllerGenerator))
                  .AddParameter([.. methodSymbol.Parameters.Select(p => $"{CreateMethodParameterOriginAttribute(httpMethod)}{p.Type.ToDisplayString()} {p.Name}")])
                  .Lambda($"proxyService.{methodSymbol.Name}({string.Join(", ", methodSymbol.Parameters.Select(p => p.Name))});");

            string CreateMethodParameterOriginAttribute(string method)
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
        }

        private static FieldBuilder BuildLocalField(INamedTypeSymbol interfaceSymbol)
        {
            // private readonly <SERVICE_TYPE> proxyService;
            //return FieldDeclaration(VariableDeclaration(IdentifierName(interfaceSymbol.ToDisplayString())).AddVariables(VariableDeclarator(Identifier("proxyService")))).AddModifiers(Token(SyntaxKind.PrivateKeyword), Token(SyntaxKind.ReadOnlyKeyword));
            return FieldBuilder.Default
                .MemberType(interfaceSymbol.ToDisplayString())
                .FieldName("proxyService");
        }

        private static ConstructorBuilder BuildConstructor(INamedTypeSymbol classSymbol, INamedTypeSymbol interfaceSymbol)
        {
            /*
             * public <SERVICE_TYPE>Controller(<SERVICE_TYPE> service)
             * {
             *     proxyService = service;
             * }
             */

            return ConstructorBuilder.Default
                    .MethodName($"{FormatClassName(classSymbol.MetadataName)}Controller")
                    .AddGeneratedCodeAttribute(typeof(ControllerGenerator))
                    .AddBody("proxyService = service;")
                    .AddParameter($"{interfaceSymbol.ToDisplayString()} service");
        }

        private static ClassBuilder CreateControllerClass(GeneratorAttributeSyntaxContext source)
        {

            var controllerAttribute = source.TargetSymbol.GetAttributes().First(a => a.AttributeClass?.ToDisplayString() == WebControllerAttributeFullName);
            var route = controllerAttribute.GetNamedValue("Route") ?? "[controller]";

            //var additionalAttribute = source.TargetSymbol.GetAttributeInitInfo<ControllerGenerator>();

            return ClassBuilder.Default
                        .ClassName($"{FormatClassName(source.TargetSymbol.MetadataName)}Controller")
                        .Modifiers("public")
                        .BaseType("global::Microsoft.AspNetCore.Mvc.ControllerBase")
                        .Attribute("global::Microsoft.AspNetCore.Mvc.ApiController")
                        .Attribute($"global::Microsoft.AspNetCore.Mvc.Route(\"api/{route}\")")
                        //.Attribute([..additionalAttribute.Select(i => i.ToString())])
                        .AddGeneratedCodeAttribute(typeof(ControllerGenerator));
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
}
