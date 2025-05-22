using System;

namespace AutoWasmApiGenerator
{
    /// <summary>
    /// 不生成控制器方法
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class WebMethodNotSupportedAttribute : Attribute
    {

    }
}
