using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace AutoWasmApiGenerator;

/// <summary>
/// API调用类中，拦截处理
/// </summary>
[Obsolete]
public interface IHttpClientHeaderHandler
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    Task SetRequestHeaderAsync(HttpRequestMessage request);
}
