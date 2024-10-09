using System.Net.Http;
using System.Threading.Tasks;

namespace AutoWasmApiGenerator;

public interface IHttpClientHeaderHandler
{
    Task SetRequestHeaderAsync(HttpRequestMessage request);
}