using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace AutoGenMapperGenerator.ReflectMapper;

internal static class DictionaryExtensions
{
    private static readonly Dictionary<Type, Delegate> conversionCache = [];

    private static object? TryParse(Type targetType, string str, CultureInfo culture)
    {
        if (conversionCache.TryGetValue(targetType, out var del))
        {
            var parser = (Func<string, CultureInfo, object?>)del;
            return parser(str, culture);
        }
        var underType = Nullable.GetUnderlyingType(targetType) ?? targetType;
        Func<string, CultureInfo, object?> parserFunc = underType switch
        {
            Type t when t == typeof(int) => (s, c) => int.Parse(s, NumberStyles.Any, c),
            Type t when t == typeof(long) => (s, c) => long.Parse(s, NumberStyles.Any, c),
            Type t when t == typeof(float) => (s, c) => float.Parse(s, NumberStyles.Any, c),
            Type t when t == typeof(double) => (s, c) => double.Parse(s, NumberStyles.Any, c),
            Type t when t == typeof(decimal) => (s, c) => decimal.Parse(s, NumberStyles.Any, c),
            Type t when t == typeof(DateTime) => (s, c) => DateTime.Parse(s, c),
            Type t when t == typeof(bool) => (s, c) => bool.Parse(s),
            _ => throw new NotSupportedException($"Type {targetType.FullName} is not supported for parsing.")
        };
        conversionCache[targetType] = parserFunc;
        return parserFunc(str, culture);
    }

    public static bool TryGetValue(IDictionary<string, object?> dict, string key, Type targetType, out object value)
    {
        value = default!;
        if (!dict.TryGetValue(key, out var dictValue))
        {
            return default!;
        }
        if (dictValue is null)
        {
            return false;
        }
        
        if (targetType == typeof(string))
        {
            value = dictValue.ToString()!;
            return true;
        }
        if (targetType == typeof(object) || targetType.IsInstanceOfType(dictValue))
        {
            value = dictValue;
            return true;
        }
        // 处理可空类型
        var underlyingType = Nullable.GetUnderlyingType(targetType);
        var conversionTargetType = underlyingType ?? targetType;
        if (conversionTargetType.IsEnum)
        {
            value = Enum.Parse(conversionTargetType, dictValue.ToString() ?? string.Empty, true);
            return true;
        }
        if (dictValue is string str)
        {
            // string转基础类型
            try
            {
                value = TryParse(conversionTargetType, str, CultureInfo.CurrentCulture)!;
                return true;
            }
            catch
            {
                return false;
            }
        }
        // 使用 Convert.ChangeType 兜底
        try
        {
            value = Convert.ChangeType(dictValue, conversionTargetType, CultureInfo.CurrentCulture);
            return true;
        }
        catch
        {
            return false;
        }
    }

}
