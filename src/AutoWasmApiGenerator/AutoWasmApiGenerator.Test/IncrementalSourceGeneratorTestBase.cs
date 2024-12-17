using Basic.Reference.Assemblies;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace AutoWasmApiGenerator.Test;

public abstract class IncrementalSourceGeneratorTestBase
{
    protected static (Compilation compilation, GeneratorDriver driver) CreateDriver(string[] source, ISourceGenerator sourceGenerator, params PortableExecutableReference[] references)
    {

        var compilation = CSharpCompilation.Create("TestProject",
            source.Select(t => CSharpSyntaxTree.ParseText(t)),
            NetStandard20.References.All.AddRange(references),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        // Create the generator driver
        var driver = CSharpGeneratorDriver.Create(
            [sourceGenerator],
            driverOptions: new GeneratorDriverOptions(disabledOutputs: IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));
        return (compilation, driver);
    }
}