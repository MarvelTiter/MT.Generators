using System;
using System.Threading;
using Microsoft.CodeAnalysis.Diagnostics;

namespace AutoWasmApiGenerator.Options;

public sealed class GlobalOptions
{
    public GlobalOptions(AnalyzerConfigOptions options)
    {
        if (options.TryGetValue("build_property.MSBuildProjectFullPath", out var projectFullPath))
        {
            ProjectFullPath = projectFullPath;
        }

        ProjectFullPath ??= string.Empty;

        GenerateInvoker =
            options.TryGetValue("build_property.AutoWasmApiGenerator_GenerateInvoker", out var generateInvoker) &&
            generateInvoker is { Length: > 0 } &&
            generateInvoker.Equals("true", StringComparison.OrdinalIgnoreCase); 

        GenerateWebController =
            options.TryGetValue("build_property.AutoWasmApiGenerator_GenerateWebController", out var generateWebController) &&
            generateWebController is { Length: > 0 } &&
            generateWebController.Equals("true", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 
    /// </summary>
    public bool GenerateInvoker { get; } 

    /// <summary>
    /// 
    /// </summary>
    public bool GenerateWebController { get; } 

    /// <summary>
    /// 
    /// </summary>
    public string? ProjectFullPath { get; }

    public static GlobalOptions Select(AnalyzerConfigOptionsProvider provider, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        return new GlobalOptions(provider.GlobalOptions);
    }
}