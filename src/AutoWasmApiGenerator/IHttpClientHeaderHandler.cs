using System;
using System.Net.Http;
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
    /// <returns></returns>
    Task SetRequestHeaderAsync(HttpRequestMessage request);
}

/// <summary>
/// <see cref="IHttpClientHeaderHandler"/>�Ŀ�ʵ��
/// </summary>
public class DefaultHttpClientHeaderHandler : IHttpClientHeaderHandler
{
    private DefaultHttpClientHeaderHandler()
    {
        
    }
    private static readonly Lazy<IHttpClientHeaderHandler> lazy = new(() => new DefaultHttpClientHeaderHandler());
    /// <summary>
    /// ����
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