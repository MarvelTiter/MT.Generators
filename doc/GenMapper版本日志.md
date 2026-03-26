# 版本功能更新记录

## v2026.03.26.1
- 🐞修复字典映射同时遇到`string`转`T`和`Nullable<T>`时出现的缓存冲突

## v2026.02.26.1
- 🐞修复构造函数已处理的属性重复处理
- ⚡️修改扩展方法的生成逻辑，同一个类中的方法放在同一个分部类中
- ⚡️扩展`MapIngoreAttribute`的用法，现在可以用到类和方法上，同时可以指定忽略那些属性

## v2026.02.25.1
- ⚡️优化扩展方法生成
    1. 同一个类中可以定义多个映射方法
    1. 自定义转换方法的第一个参数，可以选择传入源对象

```csharp
public class Contact
{
    public string? Name { get; set; }
    public string? PhoneNumber { get; set; }

    public List<MissionContact> Missions { get; set; } = [];
}

public class MissionContact
{
    public string? MissionId { get; set; }

    public string? PhoneNumber { get; set; }

    public List<Contact> Contacts { get; set; } = [];
}
public class ContactDto
{
    public string? Name { get; set; }
    public string? PhoneNumber { get; set; }

    public string? Missions { get; set; }
}
public static partial class ContactMap
{
    [GenMapper]
    [MapBetween(nameof(ContactDto.Missions), nameof(Contact.Missions), By = nameof(MissionsTransBack))]
    public static partial Contact ToContact(this ContactDto dto);

    [GenMapper]
    [MapBetween(nameof(Contact.Missions), nameof(ContactDto.Missions), By = nameof(MissionsTrans))]
    public static partial ContactDto ToDto(this Contact contact);

    // 第一个参数可以选择传入源对象
    private static List<MissionContact> MissionsTransBack(ContactDto dto, string? missions)
    {
        var items = missions?.Split(',');
        if (items?.Length > 0)
        {
            return [.. items.Select(c => new MissionContact() { MissionId = c, PhoneNumber = dto.PhoneNumber })];
        }
        else
        {
            return [];
        }
    }

    private static string MissionsTrans(List<MissionContact> missions)
    {
        return string.Join(',', missions.Select(c => c.MissionId));
    }
}
```
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
