using Microsoft.Extensions.DependencyInjection;
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
            services.AddScoped<User>();
            services.AddScoped<LogAop>();
            services.AddScoped<ExceptionAop>();
            services.AddScoped<MethodTestAop1>();
            services.AddScoped<MethodTestAop2>();
            services.AddScoped<IWrapHello, UserGeneratedProxy>();

            var provider = services.BuildServiceProvider();
            var hello = provider.GetService<IWrapHello>()!;
            var i = await hello.CountAsync("Hello");
            hello.Hello(1);
            Assert.IsTrue(5 == i);
        }
    }
}
