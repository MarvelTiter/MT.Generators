using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using AutoWasmApiGenerator.Test;
using FluentAssertions;
using Microsoft.CodeAnalysis;

namespace AutoWasmApiGenerator
{
    public partial class ControllerGeneratorTest : IncrementalSourceGeneratorTestBase
    {
        private static readonly PortableExecutableReference[] References;
        private static readonly string GeneratorVersion;

        static ControllerGeneratorTest()
        {
            References = new List<Type>
        {
            typeof(WebControllerAttribute)
        }.DistinctReferences();
            GeneratorVersion = typeof(ControllerGenerator).Assembly.GetCustomAttribute<AssemblyFileVersionAttribute>()
                ?.Version ?? throw new Exception("Unknown generator version");
            UpdateTestSuccessResult();
            UpdatePartialResult();
            UpdateVirtualResult();
        }

        private void DoTestSuccess(string source, string resultSource)
        {
            // Get AssemblyMeta from AutoWasmApiGenerator
            var (compilation, driver) = CreateDriver([source, AssemblyTag, Usings],
                new ControllerGenerator().AsSourceGenerator(), References);

            // Run the generator
            driver = driver.RunGenerators(compilation);
            var results = driver.GetRunResult();
            var result = results.Results.Single();

            // Assert that the generated sources are not empty
            Assert.NotEmpty(result.GeneratedSources);
            result.GeneratedSources.Should().HaveCount(1).And.Contain(r => r.HintName == "AutoWasmApiGenerator_Test_TestController.g.cs");
            result.GeneratedSources[0].SourceText.ToString().ReplaceLineEndings().Should()
                .BeEquivalentTo(resultSource.ReplaceLineEndings());
        }

        [Fact]
        public void Test_SuccessCode()
        {
            DoTestSuccess(TestSuccessCode, TestSuccessResult);
        }

        [Fact]
        public void Test_Partial()
        {
            DoTestSuccess(TestPartialSource, TestPartialSourceResult);
        }

        [Fact]
        public void Test_Virtual()
        {
            DoTestSuccess(TestVirtualSource, TestVirtualSourceResult);
        }


        [Fact]
        public void Test_Wag00004()
        {
            // Get AssemblyMeta from AutoWasmApiGenerator
            var (compilation, driver) = CreateDriver([TestWag00004, AssemblyTag, Usings],
                new ControllerGenerator().AsSourceGenerator(), References);

            var result = driver.RunGenerators(compilation)
                .GetRunResult();
            // result.Results.Should().BeEmpty();
            result.GeneratedTrees.Should().BeEmpty();
            result.Diagnostics.Should().HaveCount(1);
            result.Diagnostics.Should().Contain(diagnostic => diagnostic.Id == "WAG00004");
        }
    }
}
