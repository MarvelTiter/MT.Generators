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