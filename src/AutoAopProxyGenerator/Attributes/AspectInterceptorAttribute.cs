using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace AutoAopProxyGenerator
{
    public abstract class AspectInterceptorAttribute : Attribute
    {
        public abstract Task Invoke(ProxyContext context, Func<Task> process);
    }
}
