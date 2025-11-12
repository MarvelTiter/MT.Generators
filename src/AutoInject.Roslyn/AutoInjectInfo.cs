using Microsoft.CodeAnalysis;
using System.Collections.Generic;

namespace AutoInjectGenerator;

public class AutoInjectInfo(INamedTypeSymbol targetSymbol)
{
    public INamedTypeSymbol TargetSymbol { get; set; } = targetSymbol;
    public List<RegisterServiceInfo> Services { get; set; } = [];
    public string Implement { get; set; } = targetSymbol.ToDisplayString();
    public Diagnostic? Diagnostic { get; set; }
    //public string? Scoped { get; set; }
    //public string? MemberShip { get; set; }
}

public class RegisterServiceInfo(string scoped, string serviceType, string? key, string? group)
{
    public string ServiceType { get; set; } = serviceType;
    public string? Key { get; set; } = key;
    public string Scoped { get; set; } = scoped;
    public string? MemberShip { get; set; } = group;
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