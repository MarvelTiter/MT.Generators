using AutoGenMapperGenerator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoGenMapper.Test.Models;

[GenMapper]
public partial class User
{
    public int? Id { get; set; }
    public string? Name { get; set; }
    public string? Email { get; set; }
}

[GenMapperFrom(SourceType = typeof(User), Exclude = [nameof(User.Name)])]
public partial class UserDto;

//[GenMapperFrom]
//public partial class UserDtoWithoutEmail;

//public static class UserExtensions
//{
//    public static UserDto ToDto(this User user)
//    {
//        return user.MapTo<UserDto>();
//    }
//}