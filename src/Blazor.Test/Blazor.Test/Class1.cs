using AutoWasmApiGenerator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoInjectGenerator;

namespace Blazor.Test
{
    [WebController(Authorize = true)]
    [ApiInvokerGenerate]
    public interface ITest
    {
        [ApiInvokeNotSupported]
        void Log(string message);
        [WebMethod(Method = WebMethod.Get)]
        Task<bool> LogAsync(string message);
    }

    public interface IEmpty { }

    [AutoInject]
    [AutoInject(ServiceType = typeof(IEmpty))]
    public class Class1 : ITest
    {
        public void Log(string message)
        {
            throw new NotImplementedException();
        }

        public Task<bool> LogAsync(string message)
        {
            return Task.FromResult(message.Length > 5);
        }
    }


    [AutoInjectContext]
    public static partial class AutoInjectContext
    {
        [AutoInjectConfiguration(Include = "SERVER")]
        public static partial void Inject(this IServiceCollection services);
    }

    [AutoInjectContext]
    public static partial class AutoWasmInjectContext
    {
        [AutoInjectConfiguration(Include = "WASM")]
        public static partial void AutoWasm(this IServiceCollection services);
    }
}
