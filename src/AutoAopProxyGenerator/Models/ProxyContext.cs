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
/// <summary>
/// AOP过程上下文
/// </summary>
public record ProxyContext
{
    /// <summary>
    /// 代理方法返回值 
    /// </summary>
    public object? ReturnValue { get; internal set; }
    /// <summary>
    /// 代理方法参数
    /// </summary>
    public object?[] Parameters { get; set; } = [];
    /// <summary>
    /// 代理方法执行状态
    /// </summary>
    public ExecuteStatus Status { get; internal set; }
    /// <summary>
    /// 服务(接口)类型
    /// </summary>
    [NotNull] public Type? ServiceType { get; set; }
    /// <summary>
    /// 实现类型
    /// </summary>
    [NotNull] public Type? ImplementType { get; set; }
    /// <summary>
    /// 服务(接口)方法
    /// </summary>
    [NotNull] public MethodInfo? ServiceMethod { get; set; }
    /// <summary>
    /// 实现方法
    /// </summary>
    [NotNull] public MethodInfo? ImplementMethod { get; set; }
}
