using System;

namespace AutoWasmApiGenerator
{
    /// <summary>
    /// 不允许调用
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class ApiInvokeNotSupportedAttribute : Attribute
    {

    }
}
