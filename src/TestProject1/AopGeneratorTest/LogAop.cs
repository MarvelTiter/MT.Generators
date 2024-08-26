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
        public Task Invoke(ProxyContext context, Func<Task> process)
        {
            return process.Invoke();
        }
    }
}
