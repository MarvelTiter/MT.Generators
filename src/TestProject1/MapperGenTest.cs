using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestProject1.Models;

namespace TestProject1
{
    [TestClass]
    public class MapperGenTest
    {
        [TestMethod]
        public void AutoMode()
        {
            var p = new Product()
            {
                Id = 1000,
                Name = "Edge",
                Category = "Browser",
                ProductDate = new DateTime(2020, 02, 02)
            };
            var pd = p.MapToProductDto();
            Assert.IsTrue("Browser-Edge" == pd.Name);
        }
    }
}
