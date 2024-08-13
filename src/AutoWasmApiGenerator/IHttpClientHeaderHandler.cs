using System.Net.Http;

namespace AutoWasmApiGenerator;

public interface IHttpClientHeaderHandler
{
    void SetRequestHeader(HttpRequestMessage request);
}