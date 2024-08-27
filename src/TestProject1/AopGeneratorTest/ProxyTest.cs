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
            services.AddScoped<IHello, User>();
            services.AddScoped<LogAop>();
            //services.AddScoped<IHello, UserGeneratedProxy>();

            var provider = services.BuildServiceProvider();
            var hello = provider.GetService<IHello>()!;
            var i = await hello.CountAsync();
            Assert.IsTrue(3 == i);
        }
    }
}
