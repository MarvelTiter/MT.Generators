using System;

namespace AutoWasmApiGenerator;

/// <summary>
/// 配置Controller Action
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class WebMethodAttribute : Attribute
{
    /// <summary>
    /// 指定请求方法，默认为Post
    /// </summary>
    public WebMethod Method { get; set; } = WebMethod.Post;
    /// <summary>
    /// 指定Action名称，默认为方法名称
    /// </summary>
    public string? Route { get; set; }
    /// <summary>
    /// 是否支持匿名访问, 会覆盖Authorize设置
    /// </summary>
    public bool AllowAnonymous { get; set; }
    /// <summary>
    /// 是否需要授权
    /// </summary>
    public bool Authorize { get; set; }
    /// <summary>
    /// 是否生成虚方法
    /// </summary>
    public bool Virtual { get; set; }
}
