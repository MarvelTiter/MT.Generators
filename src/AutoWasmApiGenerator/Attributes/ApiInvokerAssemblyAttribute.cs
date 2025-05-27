using System;

namespace AutoWasmApiGenerator;

/// <summary>
/// 指定API调用类生成位置
/// </summary>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
public class ApiInvokerAssemblyAttribute : Attribute
{
    /// <summary>
    /// <para>当接口调用发生异常时，需要返回一个有实际意义的返回值</para>
    /// <para>假设接口统一了返回值，例如ApiResponse，具有一个是否成功的标识，可以设置该值，辅助生成器创建ApiResponse</para>
    /// <para>如果有多个值，用 逗号|空格|分号 隔开</para>
    /// </summary>
    public string? SuccessFlag { get; set; }
    /// <summary>
    /// <para>当接口调用发生异常时，需要返回一个有实际意义的返回值</para>
    /// <para>假设接口统一了返回值，例如ApiResponse，具有一个错误信息的属性，可以设置该值，辅助生成器创建ApiResponse</para>
    /// <para>如果有多个值，用 逗号|空格|分号 隔开</para>
    /// </summary>
    public string? MessageFlag { get; set; }
}
