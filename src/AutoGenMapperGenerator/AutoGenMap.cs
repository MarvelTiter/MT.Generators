using System;
using System.Collections.Generic;
using System.Text;

namespace AutoGenMapperGenerator;

/// <summary>
/// 
/// </summary>
public static class AutoGenMap
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TTarget"></typeparam>
    /// <typeparam name="TFrom"></typeparam>
    /// <param name="source"></param>
    /// <returns></returns>
    public static TTarget Map<TTarget, TFrom>(TFrom source)
        where TTarget : new()
    {
        if (source is IAutoMap smap)
        {
            return smap.MapTo<TTarget>();
        }
        var t = new TTarget();
        if (t is IAutoMap tmap)
        {
            tmap.MapFrom(source);
        }
        return t;
    }
}
