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
        public void MapFromTest()
        {

        }
    }
}
