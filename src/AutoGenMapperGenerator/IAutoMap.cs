namespace AutoGenMapperGenerator;


public interface IAutoMap
{
    object MapTo(string? target = null);
}

public static class IAutoMapExtensions
{
    public static T MapTo<T>(this IAutoMap autoMap, string? target = null)
    {
        return (T)autoMap.MapTo(target);
    }
}