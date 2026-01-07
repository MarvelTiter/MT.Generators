namespace AutoGenMapperGenerator;

/// <summary>
/// 由生成器动态实现<see cref="GenMapperAttribute"/>
/// </summary>
public interface IAutoMap
{
    /// <summary>
    /// 按类型名称选择转换类型
    /// </summary>
    /// <param name="target"></param>
    /// <returns></returns>
    object MapTo(string? target = null);
    /// <summary>
    /// 从对应类型恢复当前对象的数据，并返回当前对象
    /// </summary>
    /// <param name="value"></param>
    void MapFrom(object? value);
}
/// <summary>
/// 
/// </summary>
public static class IAutoMapExtensions
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="autoMap"></param>
    /// <param name="target"></param>
    /// <returns></returns>
    public static T MapTo<T>(this IAutoMap autoMap, string? target = null)
    {
        target ??= typeof(T).Name;
        return (T)autoMap.MapTo(target);
    }

   
    // public static T MapFrom<T>(this IAutoMap autoMap, object? value)
    // {
    //   return  (T)autoMap.MapFrom(value);
    // }
}
