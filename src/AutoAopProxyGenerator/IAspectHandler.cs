using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;

namespace AutoAopProxyGenerator;

public interface IAspectHandler
{
    Task Invoke(ProxyContext context, Func<Task> process);
}

