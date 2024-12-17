using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace AutoWasmApiGenerator;

/// <summary>
/// API�������У����ش���
/// </summary>
public interface IHttpClientHeaderHandler
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="request"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    Task SetRequestHeaderAsync(HttpRequestMessage request, CancellationToken token = default);
}

/// <summary>
/// <see cref="IHttpClientHeaderHandler"/>�Ŀ�ʵ��
/// </summary>
public class DefaultHttpClientHeaderHandler : IHttpClientHeaderHandler
{
    private DefaultHttpClientHeaderHandler() { } 

    private static readonly Lazy<IHttpClientHeaderHandler> Lazy = new(() => new DefaultHttpClientHeaderHandler());
    /// <summary>
    /// ����
    /// </summary>
    public static IHttpClientHeaderHandler Default => Lazy.Value;

    /// <summary>
    /// <inheritdoc/>
    /// </summary>
    /// <param name="request"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task SetRequestHeaderAsync(HttpRequestMessage request, CancellationToken token = default)
    {
        return Task.CompletedTask;
    }
}