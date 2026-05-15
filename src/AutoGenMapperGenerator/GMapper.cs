using AutoGenMapperGenerator.ReflectMapper;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace AutoGenMapperGenerator;

/// <summary>
/// <para>G -> Generator</para>
/// </summary>
#if NET8_0_OR_GREATER
[RequiresDynamicCode("MakeGenericType on IEnumerable<>")]
#endif
public static class GMapper
{
    /// <summary>
    /// <para>首先检查是否<see cref="IAutoMap"/>，如果是，则直接调用接口</para>
    /// <para>否侧，回退到表达式动态创建映射</para>
    /// </summary>
    /// <typeparam name="TSource"></typeparam>
    /// <typeparam name="TTarget"></typeparam>
    /// <param name="source"></param>
    /// <returns></returns>
    public static TTarget Map<
#if NET8_0_OR_GREATER
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
#endif
    TSource,
#if NET8_0_OR_GREATER
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
#endif
    TTarget>(this TSource source)
    {
        if (source is null)
        {
            return default!;
        }
        if (source is IAutoMap m)
        {
            return m.MapTo<TTarget>();
        }
        var ti = ExpressionHelper.IsComplexType(typeof(TSource));
        if (ti.IsComplex && ti.IsDictionary)
        {
            throw new InvalidOperationException("请使用TEntity ToEntity<TEntity>(this IDictionary<string, object?> dict)");
        }
        if (ti.IsComplex && ti.IsEnumerable)
        {
            throw new InvalidOperationException("不支持集合类型的映射，请使用LINQ的Select方法进行映射");
        }
        return ExpressionMapper<TSource, TTarget>.Map(source);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <param name="entity"></param>
    /// <returns></returns>
    public static IDictionary<string, object?> ToDictionary<
#if NET8_0_OR_GREATER
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
#endif
    TEntity>(this TEntity entity)
    {
        if (entity is null)
        {
            return new Dictionary<string, object?>();
        }
        return ExpressionMapper<TEntity>.MapToDictionary(entity);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <param name="dict"></param>
    /// <returns></returns>
    public static TEntity ToEntity<
#if NET8_0_OR_GREATER
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
#endif
    TEntity>(this IDictionary<string, object?> dict)
    {
        return ExpressionMapper<TEntity>.MapFromDictionary(dict);
    }
}
