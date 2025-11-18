using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace AutoWasmApiGenerator;

/// <summary>
/// <see cref="IHttpClientHeaderHandler"/>的空实现
/// </summary>
[Obsolete]
public class DefaultHttpClientHeaderHandler : IHttpClientHeaderHandler
{
    private DefaultHttpClientHeaderHandler()
    {

    }
    private static readonly Lazy<IHttpClientHeaderHandler> lazy = new(() => new DefaultHttpClientHeaderHandler());
    /// <summary>
    /// 单例
    /// </summary>
    public static IHttpClientHeaderHandler Default => lazy.Value;
    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    public Task SetRequestHeaderAsync(HttpRequestMessage request)
    {
        return Task.CompletedTask;
    }
}
