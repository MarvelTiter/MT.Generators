
#if NETSTANDARD2_0_OR_GREATER
namespace System.Diagnostics.CodeAnalysis;

/// <summary>
/// 
/// </summary>
[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.ReturnValue, AllowMultiple = false)]
public sealed class NotNullAttribute : Attribute { }

/// <summary>
/// 
/// </summary>
[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.ReturnValue, AllowMultiple = false)]
public sealed class NotNullWhenAttribute : Attribute
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="returnValue"></param>
    public NotNullWhenAttribute(bool returnValue) { }
}

#endif