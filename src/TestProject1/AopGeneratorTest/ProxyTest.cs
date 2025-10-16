using AutoAopProxyGenerator;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestProject1.AopGeneratorTest
{
    [TestClass]
    public class ProxyTest
    {
        [TestMethod]
        public void TestReturn()
        {

        }

        [TestMethod]
        public async Task TestReturnAsync()
        {
            var services = new ServiceCollection();
            services.AddScoped<IWrapHello, User>();
            services.AddScoped<LogAop>();
            services.AddScoped<ExceptionAop>();
            services.AddScoped<MethodTestAop1>();
            services.AddScoped<MethodTestAop2>();
            AutoAopProxyServiceProviderFactory providerFactory = new();
            var provider = providerFactory.CreateServiceProvider(services);
            var hello = provider.GetService<IWrapHello>()!;
            var i = await hello.CountAsync("Hello");
            hello.Hello(1);
            Assert.IsTrue(5 == i);
        }

        [TestMethod]
        public void TestExplicitMethod()
        {
            var services = new ServiceCollection();
            services.AddScoped<IUser, User2>();
            services.AddScoped<IUser1, User2>();
            services.AddScoped<IUser2, User2>();
            AutoAopProxyServiceProviderFactory providerFactory = new();
            var provider = providerFactory.CreateServiceProvider(services);
            var u1 = provider.GetService<IUser1>();
            var u2 = provider.GetService<IUser2>();
            var r1 = u1?.Hello1("Marvel1");
            var r2 = u2?.Hello2("Marvel2");
            Assert.IsTrue(r1 == "Hello Marvel1 From IUser1");
            Assert.IsTrue(r2 == "Hello Marvel2 From IUser2");
        }
    }
}
