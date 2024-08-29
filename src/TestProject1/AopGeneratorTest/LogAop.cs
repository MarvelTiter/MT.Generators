using AutoAopProxyGenerator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestProject1.AopGeneratorTest
{
    public class LogAop : IAspectHandler
    {
        public async Task Invoke(ProxyContext context, Func<Task> process)
        {
            Console.WriteLine("LogAop Before");
            await process.Invoke();
            Console.WriteLine("LogAop After");
        }
    }

    public class ExceptionAop : IAspectHandler
    {
        public async Task Invoke(ProxyContext context, Func<Task> process)
        {
            Console.WriteLine("ExceptionAop Before");
            await process.Invoke();
            Console.WriteLine("ExceptionAop After");
        }
    }
}
