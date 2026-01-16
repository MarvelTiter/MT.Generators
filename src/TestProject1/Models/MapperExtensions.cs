using AutoGenMapperGenerator;
using System.Xml.Linq;

namespace TestProject1.Models;

internal static partial class MapperExtensions
{
    [GenMapper]
    [MapBetween([nameof(Product.Name), nameof(Product.Category)], nameof(ProductDto.Name), By = nameof(MapToDtoName))]
    [MapBetween(nameof(Product.SplitValue), [nameof(ProductDto.S1), nameof(ProductDto.S2)], By = nameof(MapOneToMultiTest))]
    public static partial ProductDto ToDto(this Product product, Action<Product, ProductDto>? action = null);

    public static string MapToDtoName(string name, string category)
    {
        return $"{name}-{category}";
    }

    public static (string, string) MapOneToMultiTest(string value)
    {
        var val = value.Split(',');
        return (val[0], val[1]);
    }
}

//static partial class MapperExtensions
//{
//    static partial ProductDto ToDto(Product product)
//    {
//        throw new NotImplementedException();
//    }
//}