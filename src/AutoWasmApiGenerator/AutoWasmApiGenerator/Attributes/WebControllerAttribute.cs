using System;

namespace AutoWasmApiGenerator;

/// <summary>
/// 指示生成Controller
/// </summary>
[AttributeUsage(AttributeTargets.Interface, AllowMultiple = false)]
public class WebControllerAttribute : Attribute
{
    /// <summary>
    /// 指定Route，默认为服务类型名称
    /// </summary>
    public string? Route { get; set; }
    /// <summary>
    /// 
    /// </summary>
    public Type[] Attributes { get; set; } = [];

    /// <summary>
    /// 是否需要授权才可以访问
    /// </summary>
    public bool Authorize { get; set; }
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="attributes"></param>
    public WebControllerAttribute(params Type[] attributes)
    {
        Attributes = attributes;
    }
}
