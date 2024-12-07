using FluentAssertions;
using Generators.Shared;
using Microsoft.CodeAnalysis;

namespace AutoWasmApiGenerator.Test;

public static class ExecutableReferenceExtension
{
    public static PortableExecutableReference[] DistinctReferences(this IEnumerable<Type> types)
    {
        return types.Select(t => t.Assembly.Location).Distinct().Select(t => MetadataReference.CreateFromFile(t))
            .ToArray();
    }
}

public partial class HttpServiceInvokerGeneratorTest : IncrementalSourceGeneratorTestBase
{
    private static readonly PortableExecutableReference[] References;

    static HttpServiceInvokerGeneratorTest()
    {
        References = new List<Type>
        {
            typeof(HttpServiceInvokerGenerator),
            typeof(WebControllerAttribute)
        }.DistinctReferences();
    }

    [Fact]
    public void Test_ReferenceValidity()
    {
        var (compilation, _) = CreateDriver([TestSuccessCode, AssemblyTag, Usings],
            new HttpServiceInvokerGenerator().AsSourceGenerator(), References);

        var ret = compilation.GetAllSymbols(typeof(WebControllerAttribute).FullName!);
        ret.Should().HaveCount(1);
    }

    [Fact]
    public void Test_SuccessGenerate()
    {
        // Get AssemblyMeta from AutoWasmApiGenerator
        var (compilation, driver) = CreateDriver([TestSuccessCode, AssemblyTag, Usings],
            new HttpServiceInvokerGenerator().AsSourceGenerator(), References);

        // Run the generator
        driver = driver.RunGenerators(compilation);
        var results = driver.GetRunResult();
        var result = results.Results.Single();

        // Assert that the generated sources are not empty
        Assert.NotEmpty(result.GeneratedSources);
        result.GeneratedSources.Should().HaveCount(1).And.Contain(r => r.HintName == "AutoWasmApiGenerator_Test_TestApiInvoker.g.cs");
        result.GeneratedSources[0].SourceText.ToString().ReplaceLineEndings().Should()
            .BeEquivalentTo(TestSuccessResult.ReplaceLineEndings());
    }

    [Fact]
    public void Test_Wag00002()
    {
        // Get AssemblyMeta from AutoWasmApiGenerator
        var (compilation, driver) = CreateDriver([TestWag00002, AssemblyTag, Usings],
            new HttpServiceInvokerGenerator().AsSourceGenerator(), References);
        // TODO: Add the test, however current error code is not used
    }

    /// <summary>
    /// 测试：方法参数过多
    /// </summary>
    [Fact]
    public void Test_Wag00003()
    {
        // Get AssemblyMeta from AutoWasmApiGenerator
        var (compilation, driver) = CreateDriver([TestWag00003, AssemblyTag, Usings],
            new HttpServiceInvokerGenerator().AsSourceGenerator(), References);
        // TODO: Add the test, however current error code is not used
    }

    /// <summary>
    /// 测试：仅支持异步方法
    /// </summary>
    [Fact]
    public void Test_Wag00005()
    {
        // Get AssemblyMeta from AutoWasmApiGenerator
        var (compilation, driver) = CreateDriver([TestWag00005, AssemblyTag, Usings],
            new HttpServiceInvokerGenerator().AsSourceGenerator(), References);
        var result = driver.RunGenerators(compilation)
            .GetRunResult();
        // result.Results.Should().BeEmpty();
        result.GeneratedTrees.Should().BeEmpty();
        result.Diagnostics.Should().HaveCount(1);
        result.Diagnostics.Should().Contain(diagnostic => diagnostic.Id == "WAG00005");
    }

    /// <summary>
    /// 测试：路由中未包含路由参数
    /// </summary>
    /// <exception cref="NotImplementedException"></exception>
    [Fact]
    public void Test_Wag00006_Fail()
    {
        // Get AssemblyMeta from AutoWasmApiGenerator
        var (compilation, driver) = CreateDriver([TestWag00006, AssemblyTag, Usings],
            new HttpServiceInvokerGenerator().AsSourceGenerator(), References);
        var results = driver.RunGenerators(compilation)
                 .GetRunResult();
        results.Diagnostics.Should()
            .HaveCount(1)
            .And.Contain(diagnostic => diagnostic.Id == "WAG00006");
        results.GeneratedTrees.Should().BeEmpty();
    }

    /// <summary>
    /// 测试：路由中未包含路由参数
    /// </summary>
    /// <exception cref="NotImplementedException"></exception>
    [Fact]
    public void Test_FromRoute()
    {
        // Get AssemblyMeta from AutoWasmApiGenerator
        var (compilation, driver) = CreateDriver([TestFromRoute, AssemblyTag, Usings],
            new HttpServiceInvokerGenerator().AsSourceGenerator(), References);
        var results = driver.RunGenerators(compilation)
                 .GetRunResult();
        results.Diagnostics.Should().BeEmpty();
        results.GeneratedTrees.Should().NotBeEmpty();
        results.Results.Should().HaveCount(1);
        var result = results.Results[0];
        result.GeneratedSources.Should().HaveCount(1);
        string generated = result.GeneratedSources[0].SourceText.ToString().ReplaceLineEndings();
        generated.Should().BeEquivalentTo(TestFromRouteResult.ReplaceLineEndings());
    }


    [Fact]
    public void Test_Wag00007()
    {
        // Get AssemblyMeta from AutoWasmApiGenerator
        var (compilation, driver) = CreateDriver([TestWag00007, AssemblyTag, Usings],
            new HttpServiceInvokerGenerator().AsSourceGenerator(), References);
        var results = driver.RunGenerators(compilation)
                 .GetRunResult();
        results.Diagnostics.Should()
            .HaveCount(1)
            .And.Contain(diagnostic => diagnostic.Id == "WAG00007");
        results.GeneratedTrees.Should().BeEmpty();
    }

    /// <summary>
    /// 测试：不能设置多个[FromBody]
    /// </summary>
    /// <exception cref="NotImplementedException"></exception>
    [Fact]
    public void Test_Wag00008()
    {
        // Get AssemblyMeta from AutoWasmApiGenerator
        var (compilation, driver) = CreateDriver([TestWag00008, AssemblyTag, Usings],
            new HttpServiceInvokerGenerator().AsSourceGenerator(), References);
        var results = driver.RunGenerators(compilation)
                 .GetRunResult();
        results.Diagnostics.Should()
            .HaveCount(1)
            .And.Contain(diagnostic => diagnostic.Id == "WAG00008");
        results.GeneratedTrees.Should().BeEmpty();
    }

    /// <summary>
    /// 测试：暂不支持的返回值类型
    /// </summary>
    [Fact]
    public void Test_Wag00009()
    {
        // Get AssemblyMeta from AutoWasmApiGenerator
        var (compilation, driver) = CreateDriver([TestWag00009_0, AssemblyTag, Usings],
            new HttpServiceInvokerGenerator().AsSourceGenerator(), References);
        var results = driver.RunGenerators(compilation)
            .GetRunResult();
        results.Diagnostics.Should()
            .HaveCount(1)
            .And.Contain(diagnostic => diagnostic.Id == "WAG00009");
        results.GeneratedTrees.Should().BeEmpty();

        // Test valid return type, wag00009 should not be triggered
        (compilation, driver) = CreateDriver([TestWag00009_ValidReturnType, AssemblyTag, Usings],
            new HttpServiceInvokerGenerator().AsSourceGenerator(), References);

        results = driver.RunGenerators(compilation).GetRunResult();
        results.Diagnostics.Should()
            .BeEmpty();
        results.Results.Should().HaveCount(1);
        var result = results.Results[0];

        result.GeneratedSources.Should().HaveCount(1);
        result.GeneratedSources[0].SourceText.ToString().ReplaceLineEndings().Should()
            .BeEquivalentTo(TestWag00009_ValidReturnTypeResult.ReplaceLineEndings());
    }

    [Fact]
    public void Test_Wag00010()
    {
        // Get AssemblyMeta from AutoWasmApiGenerator
        var (compilation, driver) = CreateDriver([TestWag00010, AssemblyTag, Usings],
            new HttpServiceInvokerGenerator().AsSourceGenerator(), References);
        // TODO: Add the test, however current error code may not be triggered
    }

    [Fact]
    public void Test_Wag00011()
    {
        // Get AssemblyMeta from AutoWasmApiGenerator
        var (compilation, driver) = CreateDriver([TestWag00011, AssemblyTag, Usings],
            new HttpServiceInvokerGenerator().AsSourceGenerator(), References);
        var results = driver.RunGenerators(compilation)
            .GetRunResult();
        results.Diagnostics.Should()
            .HaveCount(1)
            .And.Contain(diagnostic => diagnostic.Id == "WAG00011");
        results.GeneratedTrees.Should().BeEmpty();
    }


    #region Example

    /* // this just an example snippet, not a real test
    public void Test_Example()
    {
        // Get AssemblyMeta from AutoWasmApiGenerator
        var (compilation, driver) = CreateDriver([Test0, AssemblyTag, Usings],
            new HttpServiceInvokerGenerator().AsSourceGenerator(), References);

        // Run the generator
        driver = driver.RunGenerators(compilation);
        var results = driver.GetRunResult();
        var result = results.Results.Single();
        //
        // Log the diagnostics
        foreach (var diagnostic in result.Diagnostics)
        {
            Console.WriteLine(diagnostic.ToString());
        }

        // Log the generated sources
        foreach (var generatedSource in result.GeneratedSources)
        {
            Console.WriteLine($"HintName: {generatedSource.HintName}");
            Console.WriteLine(generatedSource.SourceText.ToString());
        }

        // Assert that the generated sources are not empty
        Assert.NotEmpty(result.GeneratedSources);

        // Update the compilation and rerun the generator
        compilation = compilation.AddSyntaxTrees(CSharpSyntaxTree.ParseText("// dummy"));
        driver = driver.RunGenerators(compilation);

        // Assert the driver doesn't recompute the output
        results = driver.GetRunResult();
        result = results.Results.Single();
        var allOutputs = result.TrackedOutputSteps.SelectMany(outputStep => outputStep.Value)
            .SelectMany(output => output.Outputs);
        Assert.Collection(allOutputs, output => Assert.Equal(IncrementalStepRunReason.Cached, output.Reason));

        // Assert the driver use the cached result from AssemblyName and Syntax
        var assemblyNameOutputs = result.TrackedSteps["AssemblyName"].Single().Outputs;
        Assert.Collection(assemblyNameOutputs,
            output => Assert.Equal(IncrementalStepRunReason.Unchanged, output.Reason));

        var syntaxOutputs = result.TrackedSteps["Syntax"].Single().Outputs;
        Assert.Collection(syntaxOutputs, output => Assert.Equal(IncrementalStepRunReason.Unchanged, output.Reason));
    }
    */

    #endregion
}