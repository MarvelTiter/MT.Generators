using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace AutoAopProxyGenerator
{
    /// <summary></summary>
    public class AutoAopProxyServiceProviderFactory : IServiceProviderFactory<IServiceCollection>
    {
        /// <summary></summary>
        public IServiceCollection CreateBuilder(IServiceCollection services)
        {
            return services;
        }

        /// <summary></summary>
        public IServiceProvider CreateServiceProvider(IServiceCollection containerBuilder)
        {
            IServiceCollection serviceCollection = new ServiceCollection();
            foreach (var sd in containerBuilder)
            {
                //service.
                var implType = GetImplType(sd);
                if (implType?.GetCustomAttribute<GenAspectProxyAttribute>() != null)
                {
                    var proxyType = implType.Assembly.GetType($"{implType.FullName}GeneratedProxy");
                    if (proxyType != null)
                    {
                        AddServiceDescriptors(serviceCollection, sd, implType, proxyType);
                        continue;
                    }
                }
                serviceCollection.Add(sd);
            }
            return serviceCollection.BuildServiceProvider();

            static Type? GetImplType(ServiceDescriptor sd)
            {
#if NET8_0_OR_GREATER
                if (sd.IsKeyedService)
                {
                    if (sd.KeyedImplementationType != null)
                    {
                        return sd.KeyedImplementationType;
                    }
                    else if (sd.KeyedImplementationInstance != null)
                    {
                        return sd.KeyedImplementationInstance.GetType();
                    }
                    else if (sd.KeyedImplementationFactory != null)
                    {
                        var typeArguments = sd.KeyedImplementationFactory.GetType().GenericTypeArguments;

                        return typeArguments[1];
                    }

                    return null;
                }
#endif
                if (sd.ImplementationType != null)
                {
                    return sd.ImplementationType;
                }
                else if (sd.ImplementationInstance != null)
                {
                    return sd.ImplementationInstance.GetType();
                }
                else if (sd.ImplementationFactory != null)
                {
                    var typeArguments = sd.ImplementationFactory.GetType().GenericTypeArguments;

                    return typeArguments[1];
                }

                return null;
            }
        }


        private static void AddServiceDescriptors(IServiceCollection serviceCollection, ServiceDescriptor sd, Type implType, Type proxyType)
        {
            // 将原实现注册为自身，在代理类中注入
            var nsd = sd.IsKeyedService
                ? ServiceDescriptor.DescribeKeyed(
                    implType,
                    sd.ServiceKey,
                    implType,
                    sd.Lifetime
                    )
                : ServiceDescriptor.Describe(
                    implType,
                    implType,
                    sd.Lifetime
                    );
            serviceCollection.Add(nsd);

            var proxySd = sd.IsKeyedService
                ? ServiceDescriptor.DescribeKeyed(
                    sd.ServiceType,
                    sd.ServiceKey,
                    proxyType,
                    sd.Lifetime
                    )
                : ServiceDescriptor.Describe(
                    sd.ServiceType,
                    proxyType,
                    sd.Lifetime
                    );

            serviceCollection.Add(nsd);
            serviceCollection.Add(proxySd);
        }
    }
}
