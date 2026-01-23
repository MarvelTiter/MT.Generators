using AutoGenMapperGenerator.ReflectMapper;
using System.Collections;
using System.Collections.Generic;

namespace AutoGenMapperGenerator;

/// <summary>
/// <para>G -> Generator</para>
/// </summary>
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
    public static TTarget Map<TSource, TTarget>(TSource source)
    {
        if (source is IAutoMap m)
        {
            return m.MapTo<TTarget>();
        }
        return ExpressionMapper<TSource, TTarget>.Map(source);
    }
}
