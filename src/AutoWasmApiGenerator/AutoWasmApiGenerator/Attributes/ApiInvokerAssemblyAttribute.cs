using System;

namespace AutoWasmApiGenerator;

/// <summary>
/// 指定API调用类生成位置
/// </summary>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
public class ApiInvokerAssemblyAttribute : Attribute
{

}
