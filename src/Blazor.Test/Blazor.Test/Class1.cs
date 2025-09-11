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
    public interface ITest
    {
        [ApiInvokeNotSupported]
        void Log(string message);
        [WebMethod(Method = WebMethod.Get)]
        Task<bool> LogAsync(string message);

        [WebMethod(Method = WebMethod.Post)]
        Task<bool> Log2Async([WebMethodParameterBinding(BindingType.FromBody)] string message, [WebMethodParameterBinding(BindingType.Ignore)] CancellationToken token);

        [WebMethod(Method = WebMethod.Delete)]
        Task<bool> Log3Async([WebMethodParameterBinding(BindingType.FromBody)] string message, [WebMethodParameterBinding(BindingType.FromQuery)] string path, CancellationToken token);
    }

    public interface IEmpty { }

    [AutoInject]
    //[AutoInject(ServiceType = typeof(IEmpty))]
    [AutoInjectSelf]
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

        public Task<bool> Log2Async(string message, CancellationToken token)
        {
            return Task.FromResult(message.Length > 5);
        }

        public Task<bool> Log3Async(string message, string path, CancellationToken token)
        {
            throw new NotImplementedException();
            return Task.FromResult(message.Length > 5);
        }
    }
}
