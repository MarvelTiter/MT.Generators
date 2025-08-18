namespace AutoGenMapperGenerator;

/// <summary>
/// 自定义映射配置
/// </summary>
/// <typeparam name="TSource"></typeparam>
/// <typeparam name="TTarget"></typeparam>
public abstract class AutoMapConfiguration<TSource, TTarget>
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="source"></param>
    /// <param name="target"></param>
    public abstract void MapTo(TSource source, TTarget target);
    /// <summary>
    /// 
    /// </summary>
    /// <param name="source"></param>
    /// <param name="target"></param>
    public abstract void MapFrom(TSource source, TTarget target);
}
