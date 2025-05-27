using System;
using System.Threading.Tasks;

namespace AutoWasmApiGenerator;

/// <summary>
/// <see cref="IGeneratedApiInvokeDelegatingHandler"/>的空实现
/// </summary>
public class GeneratedApiInvokeDelegatingHandler : IGeneratedApiInvokeDelegatingHandler
{
    private static readonly Lazy<IGeneratedApiInvokeDelegatingHandler> lazy = new(() => new GeneratedApiInvokeDelegatingHandler());
    /// <summary>
    /// 单例
    /// </summary>
    public static IGeneratedApiInvokeDelegatingHandler Default => lazy.Value;
    /// <summary>
    /// 
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public virtual Task BeforeSendAsync(SendContext context) => Task.CompletedTask;
    /// <summary>
    /// 
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public virtual Task AfterSendAsync(SendContext context) => Task.CompletedTask;
    /// <summary>
    /// 
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public virtual Task OnExceptionAsync(ExceptionContext context) => Task.CompletedTask;
}