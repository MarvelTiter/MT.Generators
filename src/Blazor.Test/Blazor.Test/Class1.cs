using AutoWasmApiGenerator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Blazor.Test
{
    public interface ITest
    {
        void Log(string message);
        Task<bool> LogAsync(string message);
    }
    [WebController]
    [ApiInvokerGenera]
    public class Class1 : ITest
    {
        public void Log(string message)
        {
            throw new NotImplementedException();
        }

        public Task<bool> LogAsync(string message)
        {
            throw new NotImplementedException();
        }
    }


    [AutoInjectGenerator.AutoInjectContext]
    public static partial class AutoInjectContext
    {
        //[AutoInjectGenerator.]
        public static partial void Inject(this IServiceCollection services);
    }
}
