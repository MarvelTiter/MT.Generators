namespace AutoGenMapperGenerator;

/// <summary>
/// 
/// </summary>
public interface IAutoMap
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="target"></param>
    /// <returns></returns>
    object MapTo(string? target = null);
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
}