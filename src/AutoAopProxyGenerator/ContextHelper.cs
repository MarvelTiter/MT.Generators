using System;
using System.Collections.Concurrent;
using System.Linq;

namespace AutoAopProxyGenerator;

public static class ContextHelper<TService, TImpl>
{
    private static readonly ConcurrentDictionary<string, ProxyContext> caches = [];
    private static readonly Type ServiceType = typeof(TService);
    private static readonly Type ImplType = typeof(TImpl);
    public static ProxyContext GetOrCreate(string methodName, Type[] types)
    {
        var key = $"{ServiceType.FullName}_{ImplType.FullName}_{methodName}_{string.Join("_", types.Select(t => t.Name))}";
        return caches.GetOrAdd(key, (k) =>
          {
              var serviceMethod = ServiceType.GetMethod(methodName, types);
              var implMethod = ImplType.GetMethod(methodName, types);
              return new ProxyContext()
              {
                  ServiceType = ServiceType,
                  ImplementType = ImplType,
                  ServiceMethod = serviceMethod,
                  ImplementMethod = implMethod
              };
          }) with
        { };

    }
}

