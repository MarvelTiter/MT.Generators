using System;

namespace AutoWasmApiGenerator;

/// <summary>
/// 指定Controller生成位置
/// </summary>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
public class WebControllerAssemblyAttribute : Attribute
{

}
