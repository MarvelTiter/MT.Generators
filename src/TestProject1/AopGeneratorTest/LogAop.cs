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
            Console.WriteLine($"LogAop Before: {context.Status}");
            await process.Invoke();
            Console.WriteLine($"LogAop After: {context.Status}");
        }
    }

    public class ExceptionAop : IAspectHandler
    {
        public async Task Invoke(ProxyContext context, Func<Task> process)
        {
            Console.WriteLine($"ExceptionAop Before: {context.Status}");
            await process.Invoke();
            Console.WriteLine($"ExceptionAop After: {context.Status}");
        }
    }
    public class MethodTestAop1 : IAspectHandler
    {
        public async Task Invoke(ProxyContext context, Func<Task> process)
        {
            Console.WriteLine($"MethodTestAop1 Before: {context.Status}");
            await process.Invoke();
            Console.WriteLine($"MethodTestAop1 After: {context.Status}");
        }
    }
    public class MethodTestAop2 : IAspectHandler
    {
        public async Task Invoke(ProxyContext context, Func<Task> process)
        {
            Console.WriteLine($"MethodTestAop2 Before: {context.Status}");
            await process.Invoke();
            Console.WriteLine($"MethodTestAop2 After: {context.Status}");
        }
    }
}
