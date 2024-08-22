using AutoGenMapperGenerator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestProject1.Models
{
    [GenMapper]
    [GenMapper(typeof(ProductDto))]
    internal partial class Product : IAutoMap
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Category { get; set; }
        [MapTo(Target = typeof(ProductDto), Name = nameof(ProductDto.Date))]
        public DateTime? ProductDate { get; set; }
    }

    internal class ProductDto
    {
        public static string NameMapFrom(Product p)
        {
            return $"{p.Category}-{p.Name}";
        }
        public int Id { get; set; }

        [MapFrom(Source = typeof(Product), By = nameof(NameMapFrom))]
        public string? Name { get; set; }
        //[MapFrom(Source = typeof(Product), Name = nameof(Product.ProductDate))]
        public DateTime? Date { get; set; }
    }
}
