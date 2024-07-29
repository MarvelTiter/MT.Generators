using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace AutoAopProxyGenerator
{
    public class AutoAopProxyServiceProviderFactory : IServiceProviderFactory<IServiceCollection>
    {
        public IServiceCollection CreateBuilder(IServiceCollection services)
        {
            return services;
        }

        public IServiceProvider CreateServiceProvider(IServiceCollection containerBuilder)
        {
            IServiceCollection serviceCollection = new ServiceCollection();
            foreach (var service in containerBuilder)
            {
                //ServiceDescriptor.
            }
            return serviceCollection.BuildServiceProvider();
        }
    }
}
