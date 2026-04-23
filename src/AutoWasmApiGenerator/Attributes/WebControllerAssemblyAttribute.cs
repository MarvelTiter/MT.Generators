using System;

namespace AutoWasmApiGenerator;

/// <summary>
/// 指定Controller生成位置
/// </summary>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
public class WebControllerAssemblyAttribute : Attribute
{
    /// <summary>
    /// 默认生成模式
    /// </summary>
    public enum ApiMode
    {
        /// <summary>
        /// 生成Minimal API
        /// </summary>
        MinimalApi,
        /// <summary>
        /// 生成Controller
        /// </summary>
        Controller,
    }
    /// <summary>
    /// Api生成模式
    /// </summary>
    public ApiMode Mode { get; set; } = ApiMode.Controller;
}
