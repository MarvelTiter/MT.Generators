using AutoGenMapperGenerator;

namespace Blazor.Test.Client.Models;

[GenMapper]
[GenMapper(TargetType = typeof(UserDto))]
public partial class User : IAutoMap
{
    public string Product { get; set; }
    public string Category { get; set; }
}

// public partial class User
// {
//     public object MapTo(string? target = null) 
//     {
//         if (string.IsNullOrEmpty(target))
//         {
//             throw new ArgumentNullException(nameof(target), "存在多个目标对象，请指定目标对象，推荐使用nameof(TargetType)");
//         }
//
//         if (target == nameof(User))
//         {
//             return MapToUser();
//         }
//
//         if (target == nameof(UserDto))
//         {
//             return MapToUserDto();
//         }
//
//         throw new ArgumentException("未找到指定目标的映射方法");
//     }
//
//     public User MapToUser()
//     {
//         var u = new User()
//         {
//             Product = this.Product,
//             Category = this.Category
//         };
//         throw new NotImplementedException();
//     }
//
//     public UserDto MapToUserDto()
//     {
//         var dto = new UserDto();
//         dto.Display = dto.DisplayFormatForm(this);
//         throw new NotImplementedException();
//     }
// }

public partial class UserDto
{
    [MapFrom(Source = typeof(User), Name = nameof(User.Product))]
    public string Product1 { get; set; }
    public string DisplayFormatForm(User u)
    {
        return $"{u.Category}-{u.Product}";
    }

    public void DisplayFormatTo(User u)
    {
        var values = Display.Split('-');
        u.Category = values[0];
        u.Product = values[1];
    }

    [MapFrom(Source = typeof(User), By = nameof(DisplayFormatForm))]
    [MapTo(Target = typeof(User), By = nameof(DisplayFormatTo))]
    public string Display { get; set; }
}

public partial class UserDto
{
    public User MapToUser()
    {
        var u = new User();
        DisplayFormatTo(u);
        u.Product = this.Product1;
        return u;
    }
}