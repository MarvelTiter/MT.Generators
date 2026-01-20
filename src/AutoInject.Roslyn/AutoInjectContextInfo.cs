using Microsoft.CodeAnalysis;

namespace AutoInjectGenerator;

public class AutoInjectContextInfo(INamedTypeSymbol targetSymbol)
{
    public INamedTypeSymbol TargetSymbol { get; } = targetSymbol;
    public string ClassName => TargetSymbol.MetadataName;
    public IMethodSymbol? MethodSymbol { get; set; }
    public string[] Includes { get; set; } = [];
    public string[] Excludes { get; set; } = [];
    public bool? ContainSelf { get; set; }
    public Diagnostic? Diagnostic { get; set; }
}

//namespace AutoInjectGenerator
//{
//    [Generator(LanguageNames.CSharp)]
//    public class AutoInjectContextGenerator : IIncrementalGenerator
//    {
//        const string AutoInjectContext = "AutoInjectGenerator.AutoInjectContextAttribute";
//        const string AutoInjectConfiguration = "AutoInjectGenerator.AutoInjectConfiguration";
//        const string AutoInject = "AutoInjectGenerator.AutoInjectAttribute";
//        public void Initialize(IncrementalGeneratorInitializationContext context)
//        {
//            var source = context.SyntaxProvider.ForAttributeWithMetadataName(
//                AutoInjectContext
//                , static (node, _) => node is ClassDeclarationSyntax
//                , static (ctx, _) => ctx);
//            context.RegisterSourceOutput(source, (context, source) =>
//            {
//                //var compilcation = source.SemanticModel.Compilation;
//                //var used = compilcation.GetUsedAssemblyReferences();

//                //var allContext = source.GetAllSymbols(AutoInjectContext);
//                //if (allContext.Any() == false)
//                //{
//                //    return;
//                //}

//                //foreach (var item in allContext)
//                //{
//                //    if (!EqualityComparer<IAssemblySymbol>.Default.Equals(item.ContainingAssembly, source.SourceModule.ContainingAssembly))
//                //    {
//                //        continue;
//                //    }
//                //    var all = source.GetAllSymbols(AutoInject);
//                //    CreateContextFile(context, item, all);
//                //}

//                var all = source.SemanticModel.Compilation.GetAllSymbols(AutoInject);
//                CreateContextFile(context, (INamedTypeSymbol)source.TargetSymbol, all);

//            });
//        }


//    }
//}