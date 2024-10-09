using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace AutoAopProxyGenerator;

public static class ProxyContextExtensions
{
    public static void SetReturnValue(this ProxyContext context, object value, ExecuteStatus status = ExecuteStatus.Break)
    {
        context.ReturnValue = value;
        context.Status = status;
    }
    public static void SetStatus(this ProxyContext context, ExecuteStatus status)
    {
        context.Status = status;
    }
}

public record ProxyContext
{
    public object? ReturnValue { get; internal set; }
    public object?[] Parameters { get; set; } = [];
    public ExecuteStatus Status { get; internal set; }
    [NotNull] public Type? ServiceType { get; set; }
    [NotNull] public Type? ImplementType { get; set; }
    [NotNull] public MethodInfo? ServiceMethod { get; set; }
    [NotNull] public MethodInfo? ImplementMethod { get; set; }
}
