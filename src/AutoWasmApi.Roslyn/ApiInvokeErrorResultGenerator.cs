//using Generators.Shared;
//using Generators.Shared.Builder;
//using Microsoft.CodeAnalysis;
//using Microsoft.CodeAnalysis.CSharp.Syntax;
//using static AutoWasmApiGenerator.GeneratorHelpers;
//namespace AutoWasmApiGenerator;

//[Generator(LanguageNames.CSharp)]
//public class ApiInvokeErrorResultGenerator : IIncrementalGenerator
//{
//    public void Initialize(IncrementalGeneratorInitializationContext context)
//    {
//        var map = context.SyntaxProvider.ForAttributeWithMetadataName(
//                  WebControllerAttributeFullName,
//                  static (node, _) => node is InterfaceDeclarationSyntax,
//                  static (c, _) => c
//                  );
//        context.RegisterSourceOutput(map, CreateCodeFile);
//    }

//    private static void CreateCodeFile(SourceProductionContext source, GeneratorAttributeSyntaxContext context)
//    {
//        var targetSymbol = (INamedTypeSymbol)context.TargetSymbol;
//        var codefile = CodeFile.New($"{targetSymbol.FormatClassName(true)}ErrorResult.g.cs").AddMembers(NamespaceBuilder.Default.Namespace(targetSymbol.ContainingNamespace.ToDisplayString()).AddMembers(ClassBuilder.Default.ClassName($"{targetSymbol.FormatClassName()}ErrorResult"))).AddUsings(targetSymbol.GetTargetUsings());
//        source.AddSource(codefile);
//    }
//}
