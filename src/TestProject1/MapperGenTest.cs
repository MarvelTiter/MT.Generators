using AutoGenMapperGenerator;
using AutoGenMapperGenerator.ReflectMapper;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestProject1.AopGeneratorTest;
using TestProject1.Models;

namespace TestProject1
{
    [TestClass]
    public class MapperGenTest
    {
        [TestMethod]
        public void AutoMode()
        {
            var model = new MappingTestModel()
            {
                Deadline = DateTime.Now,
                Last = DateTime.Now,
                Id = 1000,
                Level = "1001",
                Name = "Test",
            };
            var dto = model.MapToMappingTestModelDto();
            Assert.IsTrue(dto.Label == "1001-Test-1001");
        }

        [TestMethod]
        public void ExpressionMapEntity()
        {
            MapperOptions.Instance.ConfigProfile<Product2, Product2Dto>(profile =>
            {
                profile.ForMember(dest => dest.Date, opt => opt.ProductDate);
                profile.ForMember(dest => dest.Name, opt => $"{opt.Name} - {opt.Category}");
                profile.ForConstructor(p => p.Id);
            });
            var p = new Product2()
            {
                Id = 5,
                Name = "Product A",
                Category = "Category 1",
                ProductDate = DateTime.Now,
                SubProduct = new()
                {
                    Id = 6,
                    Name = "Product B",
                },
                Products = new List<Product2>()
                {
                    new Product2()
                    {
                        Id =7,
                        Name="Product C"
                    },
                    new Product2()
                    {
                        Id=8,
                        Name="Product D"
                    }
                }
            };

            var dto = p.Map<Product2, Product2Dto>();

        }
        class TTT
        {
            public int Id { get; set; }
            public string? Name { get; set; }
            public string? Category { get; set; }
            public DateTime? ProductDate { get; set; }
        }
        [TestMethod]
        public void TestFromDictionary()
        {
            var dateString = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var date = DateTime.Now;
            var dict = new Dictionary<string, object?>()
            {
                { "Id", 1 },
                { "Name", "Test Product" },
                { "Category", "Category A" },
                { "ProductDate", date },
            };
            var product = dict.ToEntity<Product2>();
            Assert.IsNotNull(product);
            Assert.AreEqual(1, product.Id);
            Assert.AreEqual("Test Product", product.Name);
            Assert.AreEqual("Category A", product.Category);
        }

        [TestMethod]
        public void TestToDictionary()
        {
            var p = new Product2()
            {
                Id = 5,
                Name = "Product A",
                Category = "Category 1",
                ProductDate = DateTime.Now,
                SubProduct = new()
                {
                    Id = 6,
                    Name = "Product B",
                },
                Products = new List<Product2>()
                {
                    new Product2()
                    {
                        Id =7,
                        Name="Product C"
                    },
                    new Product2()
                    {
                        Id=8,
                        Name="Product D"
                    }
                }
            };

            var dict = p.ToDictionary();
        }
    }
}
