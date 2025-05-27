using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace AutoWasmApiGenerator;

/// <summary>
/// API调用类的切面处理器
/// </summary>
public interface IGeneratedApiInvokeDelegatingHandler
{
    /// <summary>
    /// 接口调用前
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    Task BeforeSendAsync(SendContext context);
    /// <summary>
    /// 接口调用后
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    Task AfterSendAsync(SendContext context);
    /// <summary>
    /// 接口调用发生异常
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    Task OnExceptionAsync(ExceptionContext context);
}

/// <summary>
/// 接口调用上下文
/// </summary>
/// <param name="type"></param>
/// <param name="method"></param>
/// <param name="request"></param>
public class SendContext(Type type, string method, HttpRequestMessage request)
{
    /// <summary>
    /// 调用接口的目标类型
    /// </summary>
    public Type TargetType { get; set; } = type;
    /// <summary>
    /// 调用接口的目标方法
    /// </summary>
    public string TargetMethod { get; set; } = method;
    /// <summary>
    /// 调用接口的目标方法的参数
    /// </summary>
    public object?[]? Parameters { get; set; }
    /// <summary>
    /// 调用接口的返回值类型
    /// </summary>
    public Type? ReturnType { get; set; }
    /// <summary>
    /// 调用接口的请求
    /// </summary>
    public HttpRequestMessage Request { get; set; } = request;
    /// <summary>
    /// 调用接口的响应
    /// </summary>
    public HttpResponseMessage? Response { get; set; }
}

/// <summary>
/// 调用接口的异常上下文
/// </summary>
/// <param name="sendContext"></param>
/// <param name="exception"></param>
public class ExceptionContext(SendContext sendContext, Exception exception)
{
    /// <summary>
    /// 接口调用上下文
    /// </summary>
    public SendContext SendContext { get; set; } = sendContext;
    /// <summary>
    /// 接口调用异常
    /// </summary>
    public Exception Exception { get; set; } = exception;
    /// <summary>
    /// 异常是否已经处理
    /// </summary>
    public bool Handled { get; set; }
}
