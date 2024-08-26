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
        public Task TestReturnAsync()
        {
            return Task.FromResult(0);
        }
    }
}
