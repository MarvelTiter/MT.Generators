using System;

namespace AutoWasmApiGenerator
{
    /// <summary>
    /// 不生成接口调用的代码
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public class ApiInvokeNotSupportedAttribute : Attribute
    {

    }
}
