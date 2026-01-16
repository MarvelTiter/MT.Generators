using System;

namespace AutoGenMapperGenerator;

/// <summary>
/// 忽略映射
/// </summary>
[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public class MapIgnoreAttribute : Attribute
{

}
