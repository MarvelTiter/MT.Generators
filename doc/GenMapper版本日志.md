# 版本功能更新记录

## v0.1.0
- ⚡️升级`.NET10`
- 🛠优化生成器代码
- ⚡️支持生成扩展方法

```csharp
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
```

## v0.0.9

- ⚡️重新定义自定义映射规则，删除`MapToAttribute`和`MapFromAttribute`，统一在需要映射的类型上使用`MapBetweenAttribute`，并且支持反向映射功能AutoMap.MapFrom
- ⚡️`IAutoMap`新增`MapFrom`
