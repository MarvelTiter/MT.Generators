using AutoGenMapperGenerator;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

    [GenMapper]
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
    [GenMapperFromDictionary(nameof(Id))]
    internal partial class Product
    {
        public Product(int id)
        {
            Id = id;
        }
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Category { get; set; }
        [MapTo(Target = typeof(ProductDto), Name = nameof(ProductDto.Date))]
        public DateTime? ProductDate { get; set; }
        public Product? SubProduct { get; set; }
        public IEnumerable<Product> Products { get; set; } = [];

    }

    internal class ProductDto
    {
        public ProductDto(int id)
        {
            Id = id;
        }
        public string NameMapFrom(Product p)
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
