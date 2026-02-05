using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoGenMapperGenerator.ReflectMapper;

/// <summary>
/// 
/// </summary>
public interface IMapperService
{
    /// <summary>
    /// <para>首先检查是否<see cref="IAutoMap"/>，如果是，则直接调用接口</para>
    /// <para>否侧，回退到表达式动态创建映射</para>
    /// </summary>
    /// <typeparam name="TSource"></typeparam>
    /// <typeparam name="TTarget"></typeparam>
    /// <param name="source"></param>
    /// <returns></returns>
    TTarget Map<
#if NET8_0_OR_GREATER
   [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
#endif
    TSource,
#if NET8_0_OR_GREATER
   [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
#endif
    TTarget>(TSource source);

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <param name="entity"></param>
    /// <returns></returns>
    IDictionary<string, object?> ToDictionary<TEntity>(TEntity entity);

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <param name="dict"></param>
    /// <returns></returns>
    TEntity ToEntity<TEntity>(IDictionary<string, object?> dict);
}


internal sealed class MapperService : IMapperService
{
    public TTarget Map<[DynamicallyAccessedMembers((DynamicallyAccessedMemberTypes)(-1))] TSource, [DynamicallyAccessedMembers((DynamicallyAccessedMemberTypes)(-1))] TTarget>(TSource source)
    {
        if (source is null)
        {
            return default!;
        }
        if (source is IAutoMap m)
        {
            return m.MapTo<TTarget>();
        }
        var (IsComplex, IsDictionary, IsEnumerable) = ExpressionHelper.IsComplexType(typeof(TSource));
        if (IsComplex && IsDictionary)
        {
            throw new InvalidOperationException("请使用TEntity ToEntity<TEntity>(this IDictionary<string, object?> dict)");
        }
        if (IsComplex && IsEnumerable)
        {
            throw new InvalidOperationException("不支持集合类型的映射，请使用LINQ的Select方法进行映射");
        }
        return ExpressionMapper<TSource, TTarget>.Map(source);
    }

    public IDictionary<string, object?> ToDictionary<TEntity>(TEntity entity)
    {
        if (entity is null)
        {
            return new Dictionary<string, object?>();
        }
        return ExpressionMapper<TEntity>.MapToDictionary(entity);
    }

    public TEntity ToEntity<TEntity>(IDictionary<string, object?> dict)
    {
        return ExpressionMapper<TEntity>.MapFromDictionary(dict);
    }
}