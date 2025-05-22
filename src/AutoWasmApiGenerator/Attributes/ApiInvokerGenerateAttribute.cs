using System;
using System.Linq;

namespace AutoWasmApiGenerator;

/// <summary>
/// 指示生成API调用类
/// </summary>
[AttributeUsage(AttributeTargets.Interface, AllowMultiple = false)]
[Obsolete("统一使用WebControllerAttribute作为标识", true)]
public class ApiInvokerGenerateAttribute : Attribute
{
    /// <summary>
    /// 
    /// </summary>
    public Type[] Attributes { get; set; }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="attributes"></param>
    public ApiInvokerGenerateAttribute(params Type[] attributes)
    {
        Attributes = attributes;
    }
}
