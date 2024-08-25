using System;
using System.Threading.Tasks;

namespace AutoAopProxyGenerator;

public interface IAspectHandler
{
    Task Invoke(ProxyContext context, Func<Task> process);
}

