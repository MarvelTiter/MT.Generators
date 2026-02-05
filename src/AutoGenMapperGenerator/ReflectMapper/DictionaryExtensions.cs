using System;
using System.Collections.Generic;
using System.Globalization;

namespace AutoGenMapperGenerator.ReflectMapper;

internal static class DictionaryExtensions
{
    private static readonly Dictionary<Type, Delegate> conversionCache = [];

    private static T TryParse<T>(string str, CultureInfo culture)
    {
        var targetType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
        if (conversionCache.TryGetValue(targetType, out var del))
        {
            var parser = (Func<string, CultureInfo, T>)del;
            return parser(str, culture);
        }
        Func<string, CultureInfo, T> parserFunc = targetType switch
        {
            Type t when t == typeof(int) => (s, c) => (T)(object)int.Parse(s, NumberStyles.Any, c),
            Type t when t == typeof(long) => (s, c) => (T)(object)long.Parse(s, NumberStyles.Any, c),
            Type t when t == typeof(float) => (s, c) => (T)(object)float.Parse(s, NumberStyles.Any, c),
            Type t when t == typeof(double) => (s, c) => (T)(object)double.Parse(s, NumberStyles.Any, c),
            Type t when t == typeof(decimal) => (s, c) => (T)(object)decimal.Parse(s, NumberStyles.Any, c),
            Type t when t == typeof(DateTime) => (s, c) => (T)(object)DateTime.Parse(s, c),
            Type t when t == typeof(bool) => (s, c) => (T)(object)bool.Parse(s),
            _ => throw new NotSupportedException($"Type {targetType.FullName} is not supported for parsing.")
        };
        conversionCache[targetType] = parserFunc;
        return parserFunc(str, culture);
    }


    public static bool TryGetValue<TValue>(this IDictionary<string, object?> dict, string key, out object value)
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
        if (typeof(TValue) == typeof(object))
        {
            value = (TValue)dictValue;
            return true;
        }
        if (typeof(TValue) == typeof(string))
        {
            value = (TValue)(object)dictValue.ToString()!;
            return true;
        }
        if (dictValue is TValue tValue)
        {
            value = tValue;
            return true;
        }
        // 处理可空类型
        var underlyingType = Nullable.GetUnderlyingType(typeof(TValue));
        var conversionTargetType = underlyingType ?? typeof(TValue);
        if (conversionTargetType.IsEnum)
        {
            value = (TValue)Enum.Parse(conversionTargetType, dictValue.ToString() ?? string.Empty, true);
            return true;
        }
        if (dictValue is string str)
        {
            // string转基础类型
            try
            {
                value = TryParse<TValue>(str, CultureInfo.CurrentCulture);
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
            var convertedValue = Convert.ChangeType(dictValue, conversionTargetType, CultureInfo.CurrentCulture);
            value = (TValue)convertedValue!;
            return true;
        }
        catch
        {
            return false;
        }
    }
}
