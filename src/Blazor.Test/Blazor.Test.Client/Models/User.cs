using AutoGenMapperGenerator;

namespace Blazor.Test.Client.Models;

[GenMapper(TargetType = typeof(UserDto))]
[MapBetween(typeof(UserDto), [nameof(Category), nameof(Product)], nameof(UserDto.Display), By = nameof(MapDisplay))]
public partial class User
{

    [MapBetween(typeof(UserDto), nameof(UserDto.Product1))]
    public string Product { get; set; }
    public string Category { get; set; }

    public static string MapDisplay(string category, string product)
    {
        return $"{category}-{product}";
    }

    public static (string, string) MapDisplay(string display)
    {
        var r = display.Split('-');
        return (r[0], r[1]);
    }
}

public partial class UserDto
{
    public string Product1 { get; set; }

    public string Display { get; set; }
}
