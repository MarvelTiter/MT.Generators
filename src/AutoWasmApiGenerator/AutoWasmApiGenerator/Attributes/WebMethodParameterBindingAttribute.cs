using System;

namespace AutoWasmApiGenerator;

/// <summary>
/// </summary>
/// <param name="bindingType"></param>
[AttributeUsage(AttributeTargets.Parameter)]
public class WebMethodParameterBindingAttribute(BindingType bindingType) : Attribute
{
    /// <summary>
    /// </summary>
    public BindingType Type { get; } = bindingType;
}
