using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;

namespace AutoAopProxyGenerator;

/// <summary>
/// AOP处理接口
/// </summary>
public interface IAspectHandler
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="context"></param>
    /// <param name="process"></param>
    /// <returns></returns>
    Task Invoke(ProxyContext context, Func<Task> process);
}

