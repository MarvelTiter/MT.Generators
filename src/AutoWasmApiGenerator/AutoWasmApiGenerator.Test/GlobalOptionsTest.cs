using System.Diagnostics.CodeAnalysis;
using AutoWasmApiGenerator.Options;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AutoWasmApiGenerator.Test;

public class GlobalOptionsTest
{
    private static readonly GlobalOptions LocalGlobalOptionsForTests = CreateOptions(new AnalyzerConfigOptionsDummy());

    private static GlobalOptions CreateOptions(AnalyzerConfigOptionsDummy dummy)
    {
        return GlobalOptions.Select(new AnalyzerConfigOptionsProviderDummy(dummy), default);
    }

    [Fact]
    public void GlobalDefault()
    {
        var globalOptions = LocalGlobalOptionsForTests;
        globalOptions.ProjectFullPath.Should().BeEmpty();
        globalOptions.GenerateInvoker.Should().BeFalse();
        globalOptions.GenerateWebController.Should().BeFalse();
    }

    [Fact]
    public void GlobalSettings_CanReadAll()
    {
        var globalOptions = CreateOptions(new AnalyzerConfigOptionsDummy()
        {
            MSBuildProjectFullPath = "projectFullPath.csproj",
            AutoWasmApiGenerator_GenerateInvoker = "true",
            AutoWasmApiGenerator_GenerateWebController = "true"
        });
        globalOptions.ProjectFullPath.Should().Be("projectFullPath.csproj");
        globalOptions.GenerateInvoker.Should().BeTrue();
        globalOptions.GenerateWebController.Should().BeTrue();
    }
}

public class AnalyzerConfigOptionsDummy : AnalyzerConfigOptions
{
    // ReSharper disable InconsistentNaming
    public string? MSBuildProjectFullPath { get; init; }
    public string? AutoWasmApiGenerator_GenerateInvoker { get; init; }
    public string? AutoWasmApiGenerator_GenerateWebController { get; init; }
    // ReSharper restore InconsistentNaming

    public override bool TryGetValue(string key, [NotNullWhen(true)] out string? value)
    {
        value = key switch
        {
            "build_property.MSBuildProjectFullPath" => MSBuildProjectFullPath,
            "build_property.AutoWasmApiGenerator_GenerateInvoker" => AutoWasmApiGenerator_GenerateInvoker,
            "build_property.AutoWasmApiGenerator_GenerateWebController" => AutoWasmApiGenerator_GenerateWebController,
            _ => null
        };
        return value != null;
    }
}

public sealed class AnalyzerConfigOptionsProviderDummy(AnalyzerConfigOptions globalOptions) : AnalyzerConfigOptionsProvider
{
    public override AnalyzerConfigOptions GetOptions(SyntaxTree tree)
    {
        throw new NotImplementedException();
    }

    public override AnalyzerConfigOptions GetOptions(AdditionalText textFile)
    {
        return GlobalOptions;
    }

    public override AnalyzerConfigOptions GlobalOptions { get; } = globalOptions;
}