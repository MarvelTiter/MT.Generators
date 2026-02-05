using AutoGenMapperGenerator;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoInjectGenerator;

namespace TestProject1.Models
{
    public interface IPower
    {
        [NotNull] string? PowerId { get; set; }
        [NotNull] string? PowerName { get; set; }
        string? ParentId { get; set; }
        int PowerLevel { get; set; }
        string? Icon { get; set; }
        string? Path { get; set; }
        int Sort { get; set; }
        bool GenerateCRUDButton { get; set; }
        IEnumerable<IPower>? Children { get; set; }
    }

    // [GenMapper]
    [AutoInject]
    public partial class Power : IPower
    {
        public string? PowerId { get; set; }
        public string? PowerName { get; set; }
        public string? ParentId { get; set; }
        public int PowerLevel { get; set; }
        public string? Icon { get; set; }
        public string? Path { get; set; }
        public int Sort { get; set; }

        [NotMapped]
        public IEnumerable<Power>? Children { get; set; }
        IEnumerable<IPower>? IPower.Children { get => Children; set => Children = value?.Cast<Power>(); }

        [NotMapped]
        public bool GenerateCRUDButton { get; set; }
    }

    [GenMapper]
    [GenMapper(typeof(ProductDto))]
    [MapBetween(typeof(ProductDto), [nameof(Name), nameof(Category)], nameof(ProductDto.Name), By = nameof(MapToDtoName))]
    [MapBetween(typeof(ProductDto), nameof(SplitValue), [nameof(ProductDto.S1), nameof(ProductDto.S2)], By = nameof(MapOneToMultiTest))]
    internal partial class Product
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Category { get; set; }
        [MapBetween(typeof(ProductDto), nameof(ProductDto.Date))]
        public DateTime? ProductDate { get; set; }
        public Product? SubProduct { get; set; }
        public IEnumerable<Product> Products { get; set; } = [];
        public string? SplitValue { get; set; }

        public static string MapToDtoName(string name, string category)
        {
            return $"{name}-{category}";
        }
        public static (string, string) MapToDtoName(string name)
        {
            var val = name.Split(',');
            return (val[0], val[1]);
        }
        public static string MapOneToMultiTest(string s1, string s2)
        {
            return $"{s1},{s2}";
        }
        public static (string, string) MapOneToMultiTest(string value)
        {
            var val = value.Split(',');
            return (val[0], val[1]);
        }
    }

    internal class ProductDto
    {
        public ProductDto(int id)
        {
            Id = id;
        }
        public int Id { get; set; }

        public string? Name { get; set; }
        //[MapFrom(Source = typeof(Product), Name = nameof(Product.ProductDate))]
        public DateTime? Date { get; set; }
        public string? S1 { get; set; }
        public string? S2 { get; set; }
        public ProductDto? SubProduct { get; set; }
        public IEnumerable<ProductDto> Products { get; set; } = [];
    }


    internal partial class Product2
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Category { get; set; }
        public DateTime? ProductDate { get; set; }
        public Product2? SubProduct { get; set; }
        public IEnumerable<Product2> Products { get; set; } = [];
        public string? SplitValue { get; set; }

    }

    internal class Product2Dto
    {
        public Product2Dto()
        {
            
        }
        public Product2Dto(int id)
        {
            Id = id;
        }
        public int Id { get; set; }

        public string? Name { get; set; }
        //[MapFrom(Source = typeof(Product), Name = nameof(Product.ProductDate))]
        public DateTime? Date { get; set; }
        public Product2Dto? SubProduct { get; set; }
        public IEnumerable<Product2Dto> Products { get; set; } = [];
    }
}
