using AutoGenMapperGenerator;
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
            var p = new Product(1000)
            {
                Name = "Edge",
                Category = "Browser",
                ProductDate = new DateTime(2020, 02, 02),
                Products = [
                    new(1001){
                        Name = "H1",
                        Category = "HTML"
                    },
                    new(1002){
                        Name = "H2",
                        Category = "HTML"
                    },
                    new(1003){
                        Name = "H3",
                        Category = "HTML"
                    }
                    ],
                SubProduct = new Product(1004)
                {
                    Name = "span"
                }
            };
            var pd = p.MapToProduct();
            p.SubProduct.Id = 1005;
        }
    }
}
