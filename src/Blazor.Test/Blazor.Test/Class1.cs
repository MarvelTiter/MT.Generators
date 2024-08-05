using AutoWasmApiGenerator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Blazor.Test
{
    [WebController]
    [ApiInvokerGenerate(typeof(AutoInjectGenerator.AutoInjectAttribute))]
    [MT.Generators.Abstraction.AttachAttributeArgument(typeof(ApiInvokerGenerateAttribute), typeof(AutoInjectGenerator.AutoInjectAttribute),"Group","WASM")]
    public interface ITest
    {
        [WebMethod(Method = WebMethod.Get)]
        void Log(string message);
        Task<bool> LogAsync(string message);
    }
    
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
