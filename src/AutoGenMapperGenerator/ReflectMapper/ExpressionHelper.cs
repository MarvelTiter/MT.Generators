using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq.Expressions;
using System.Reflection;

namespace AutoGenMapperGenerator.ReflectMapper;

internal static class ExpressionHelper
{
    public static (bool IsComplex, bool IsDictionary, bool IsEnumerable) IsComplexType(Type type)
    {
        if (type.IsPrimitive ||
            type == typeof(string) ||
            type == typeof(decimal) ||
            type == typeof(DateTime) ||
            type == typeof(Guid) ||
            type == typeof(TimeSpan))
        {
            return (false, false, false);
        }

        if (typeof(IDictionary<string, object>).IsAssignableFrom(type))
        {
            return (true, true, false);
        }
        var isEnumerable = IsCollectionType(type);
        return (type.IsClass && type != typeof(string), false, isEnumerable);

        static bool IsCollectionType(Type type)
        {
            // 数组
            if (type.IsArray)
            {
                return true;
            }

            // 泛型接口 IEnumerable<T>
            if (type.IsGenericType && typeof(IEnumerable<>).MakeGenericType(type.GetGenericArguments()) == type)
            {
                return true;
            }

            // 具体集合类 (List<T>, ICollection<T> 等)
            foreach (var iface in type.GetInterfaces())
            {
                if (iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                {
                    return true;
                }
            }

            return false;
        }
    }
    public static MemberInfo? GetMemberInfoFromLambda(LambdaExpression lambda)
    {
        // 去除 Convert 节点 (例如 Expression<Func<T, object>> 会产生 Convert(node))
        var body = lambda.Body is UnaryExpression unary && unary.NodeType == ExpressionType.Convert
            ? unary.Operand
            : lambda.Body;

        if (body is MemberExpression memberExpression)
        {
            return memberExpression.Member;
        }
        return null;
    }

    public static (Type, string) GetMemberTypeAndNameFromLambda(LambdaExpression lambda)
    {
        // 去除 Convert 节点 (例如 Expression<Func<T, object>> 会产生 Convert(node))
        var body = lambda.Body is UnaryExpression unary && unary.NodeType == ExpressionType.Convert
            ? unary.Operand
            : lambda.Body;
        if (body is MemberExpression memberExpression)
        {
            return (memberExpression.Type, memberExpression.Member.Name);
        }
        throw new InvalidOperationException("Lambda expression does not refer to a member.");
    }


    private static readonly MethodInfo CustomStringParseToBoolean = typeof(ExpressionMapper<,>).GetMethod(nameof(CustomStringToBoolean), BindingFlags.Public | BindingFlags.Static)!;
    private static readonly MethodInfo enumParseMethod = typeof(Enum).GetMethod("Parse", [typeof(Type), typeof(string), typeof(bool)])!;
    public static string CustomStringToBoolean(string valueString)
    {
        return ",是,1,Y,YES,TRUE,".Contains(valueString.ToUpper()) ? "True" : "False";
    }
    public static Expression GetConversionExpression(Type SourceType, Expression SourceExpression, Type TargetType, CultureInfo Culture)
    {
        Expression TargetExpression;
        if (TargetType == SourceType)
        {
            TargetExpression = SourceExpression;
        }
        else if (SourceType == typeof(string))
        {
            TargetExpression = GetParseExpression(SourceExpression, TargetType, Culture);
        }
        else if (TargetType == typeof(string))
        {
            TargetExpression = Expression.Call(SourceExpression, SourceType.GetMethod("ToString", Type.EmptyTypes)!);
        }
        else if (TargetType == typeof(bool))
        {
            MethodInfo ToBooleanMethod = typeof(Convert).GetMethod("ToBoolean", [SourceType])!;
            TargetExpression = Expression.Call(ToBooleanMethod, SourceExpression);
        }
        else if (SourceType == typeof(byte[]))
        {
            TargetExpression = GetArrayHandlerExpression(SourceExpression, TargetType);
        }
        else
        {
            TargetExpression = ConvertTypeExpression(SourceExpression, SourceType, TargetType);
            //TargetExpression = Expression.Convert(SourceExpression, TargetType);
        }
        return TargetExpression;
    }

    private static Expression GetArrayHandlerExpression(Expression sourceExpression, Type targetType)
    {
        Expression TargetExpression;
        if (targetType == typeof(byte[]))
        {
            TargetExpression = sourceExpression;
        }
        else if (targetType == typeof(MemoryStream))
        {
            ConstructorInfo ConstructorInfo = targetType.GetConstructor([typeof(byte[])])!;
            TargetExpression = Expression.New(ConstructorInfo, sourceExpression);
        }
        else
        {
            throw new Exception("Cannot convert a byte array to " + targetType.Name);
        }
        return TargetExpression;
    }
    private static Expression GetParseExpression(Expression SourceExpression, Type TargetType, CultureInfo Culture)
    {
        Type UnderlyingType = GetUnderlyingType(TargetType);
        if (UnderlyingType.IsEnum)
        {
            MethodCallExpression ParsedEnumExpression = GetEnumParseExpression(SourceExpression, UnderlyingType);
            //Enum.Parse returns an object that needs to be unboxed
            return Expression.Convert(ParsedEnumExpression, TargetType);
        }
        else
        {
            Expression ParseExpression;
            switch (UnderlyingType.FullName)
            {
                case "System.Byte":
                case "System.UInt16":
                case "System.UInt32":
                case "System.UInt64":
                case "System.SByte":
                case "System.Int16":
                case "System.Int32":
                case "System.Int64":
                case "System.Double":
                case "System.Decimal":
                    ParseExpression = GetNumberParseExpression(SourceExpression, UnderlyingType, Culture);
                    break;
                case "System.DateTime":
                    ParseExpression = GetDateTimeParseExpression(SourceExpression, UnderlyingType, Culture);
                    break;
                case "System.Boolean":
                    ParseExpression = TryParseStringToBoolean(SourceExpression, UnderlyingType);
                    break;
                case "System.Char":
                    ParseExpression = GetGenericParseExpression(SourceExpression, UnderlyingType);
                    break;
                default:
                    throw new Exception(string.Format("Conversion from {0} to {1} is not supported", "String", TargetType));
            }
            if (Nullable.GetUnderlyingType(TargetType) == null)
            {
                return ParseExpression;
            }
            else
            {
                //Convert to nullable if necessary
                return Expression.Convert(ParseExpression, TargetType);
            }
        }
        Expression GetGenericParseExpression(Expression sourceExpression, Type type)
        {
            MethodInfo ParseMetod = type.GetMethod("Parse", [typeof(string)])!;
            MethodCallExpression CallExpression = Expression.Call(ParseMetod, [sourceExpression]);
            return CallExpression;
        }
        Expression GetDateTimeParseExpression(Expression sourceExpression, Type type, CultureInfo culture)
        {
            MethodInfo ParseMetod = type.GetMethod("Parse", [typeof(string), typeof(DateTimeFormatInfo)])!;
            ConstantExpression ProviderExpression = Expression.Constant(culture.DateTimeFormat, typeof(DateTimeFormatInfo));
            MethodCallExpression CallExpression = Expression.Call(ParseMetod, [sourceExpression, ProviderExpression]);
            return CallExpression;
        }

        MethodCallExpression GetEnumParseExpression(Expression sourceExpression, Type type)
        {
            //Get the MethodInfo for parsing an Enum
            //MethodInfo EnumParseMethod = typeof(Enum).GetMethod("Parse", [typeof(Type), typeof(string), typeof(bool)])!;
            ConstantExpression TargetMemberTypeExpression = Expression.Constant(type);
            ConstantExpression IgnoreCase = Expression.Constant(true, typeof(bool));
            //Create an expression the calls the Parse method
            MethodCallExpression CallExpression = Expression.Call(enumParseMethod, [TargetMemberTypeExpression, sourceExpression, IgnoreCase]);
            return CallExpression;
        }

        MethodCallExpression GetNumberParseExpression(Expression sourceExpression, Type type, CultureInfo culture)
        {
            MethodInfo ParseMetod = type.GetMethod("Parse", [typeof(string), typeof(NumberFormatInfo)])!;
            ConstantExpression ProviderExpression = Expression.Constant(culture.NumberFormat, typeof(NumberFormatInfo));
            MethodCallExpression CallExpression = Expression.Call(ParseMetod, [sourceExpression, ProviderExpression]);
            return CallExpression;
        }
        Expression TryParseStringToBoolean(Expression sourceExpression, Type type)
        {
            var valueExpression = Expression.Call(CustomStringParseToBoolean, [sourceExpression]);
            return GetGenericParseExpression(valueExpression, type);
        }
    }
    private static Type GetUnderlyingType(Type targetType)
    {
        return Nullable.GetUnderlyingType(targetType) ?? targetType;
    }

    static MethodInfo changeType = typeof(Convert).GetMethod("ChangeType", [typeof(object), typeof(Type)])!;
    static MethodInfo isNullOrEmpty = typeof(string).GetMethod(nameof(string.IsNullOrEmpty))!;
    private static ConditionalExpression ConvertTypeExpression(Expression source, Type sourceType, Type targetType)
    {
        var underType = Nullable.GetUnderlyingType(targetType) ?? targetType;
        var isNull = Expression.Equal(source, Expression.Constant(null));
        var stringValue = Expression.Call(source, sourceType.GetMethod("ToString", Type.EmptyTypes)!);
        var isNullOrEmptyExpression = Expression.Call(isNullOrEmpty, stringValue);
        var canConvert = Expression.AndAlso(Expression.IsFalse(isNull), Expression.IsFalse(isNullOrEmptyExpression));
        var finalValue = Expression.Convert(Expression.Call(changeType, source, Expression.Constant(underType)), targetType);
        return Expression.Condition(canConvert, finalValue, Expression.Default(targetType));

    }
}
