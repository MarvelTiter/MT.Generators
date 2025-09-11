using Generators.Shared;
using Microsoft.CodeAnalysis;
using static AutoPageStateContainerGenerator.GeneratorHelper;

namespace AutoPageStateContainerGenerator;

//[Generator(LanguageNames.CSharp)]
//public class PageStateContainerServiceGenerator : IIncrementalGenerator
//{
//    public void Initialize(IncrementalGeneratorInitializationContext context)
//    {
//        context.RegisterSourceOutput(context.CompilationProvider, static (context, source) =>
//        {
//            if (source.Options.OutputKind == OutputKind.NetModule || source.Options.OutputKind == OutputKind.DynamicallyLinkedLibrary)
//            {
//                // We only want to generate code for executable projects
//                return;
//            }
//            var members = source.FindByAttributeMetadataName(STATE_CONTAINER
//                , ctx => BuildContext(ctx.TargetSymbol));
//            var asm = source.SourceModule.ContainingAssembly;
//            var file = CreateServiceCollectionExtension(asm, members);
//            context.AddSource(file);
//#if DEBUG
//            var c1 = file.ToString();
//#endif
//        });
//    }
//}
