using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace AutoAopProxyGenerator;

/// <summary></summary>
public static class ContextHelper<
#if NET8_0_OR_GREATER
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
#endif
TService,
#if NET8_0_OR_GREATER
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
#endif
TImpl>
{
    private static readonly ConcurrentDictionary<string, ProxyContext> caches = [];
#if NET8_0_OR_GREATER
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
#endif
    private static readonly Type ServiceType = typeof(TService);
#if NET8_0_OR_GREATER
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
#endif
    private static readonly Type ImplType = typeof(TImpl);
    /// <summary></summary>
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

