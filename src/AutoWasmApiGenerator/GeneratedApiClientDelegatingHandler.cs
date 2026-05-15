using System;
using System.Threading.Tasks;

namespace AutoWasmApiGenerator;

/// <summary>
/// <see cref="IGeneratedApiClientDelegatingHandler"/>的空实现
/// </summary>
public class GeneratedApiClientDelegatingHandler : IGeneratedApiClientDelegatingHandler
{
    private static readonly Lazy<IGeneratedApiClientDelegatingHandler> lazy = new(() => new GeneratedApiClientDelegatingHandler());
    /// <summary>
    /// 单例
    /// </summary>
    public static IGeneratedApiClientDelegatingHandler Default => lazy.Value;
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