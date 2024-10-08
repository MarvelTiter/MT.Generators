//using System;

//namespace AutoGenMapperGenerator;

///// <summary>
///// 拓展方法位置
///// </summary>
//[StaticMapperContext]
//public static partial class Mapper
//{
//    private static T InternalConvert<T>(object value)
//    {
//        if (value is T t)
//        {
//            return t;
//        }
//        var targetType = typeof(T);
//        return (T)Convert.ChangeType(value, Nullable.GetUnderlyingType(targetType) ?? targetType);
//    }
//}