using System;

namespace AutoInjectGenerator;

/// <summary>
/// 指定自动注册方法生成的位置
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class AutoInjectContextAttribute : Attribute
{
}
