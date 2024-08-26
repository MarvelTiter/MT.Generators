using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;

namespace AutoAopProxyGenerator;

public interface IAspectHandler
{
    Task Invoke(ProxyContext context, Func<Task> process);
}

public class ContextHelper
{
    private static readonly ConcurrentDictionary<string, ProxyContext> caches = [];
    public static ProxyContext GetOrCreate(Type @interface, Type impl, string methodName, Type[] types)
    {
        var key = $"{@interface.FullName}_{impl.FullName}_{methodName}_{string.Join("_", types.Select(t => t.Name))}";
        return caches.GetOrAdd(key, (k) =>
          {
              var interfacesMethods = @interface.GetMethod(methodName, types);
              var implMethod = impl.GetMethod(methodName, types);
              return new ProxyContext()
              {
                  ServiceType = @interface,
                  ImplementType = impl,
                  ServiceMethod = interfacesMethods,
                  ImplementMethod = implMethod
              };
          });

    }
}

