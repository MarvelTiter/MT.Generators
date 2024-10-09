using System;

namespace AutoInjectGenerator;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
public class AutoInjectConfiguration : Attribute
{
    /// <summary>
    /// 忽略<see cref="AutoInjectAttribute.Group"/>的值等于<see cref="Exclude"/>的项
    /// </summary>
    public string? Exclude { get; set; }
    /// <summary>
    /// 包括<see cref="AutoInjectAttribute.Group"/>的值等于<see cref="Include"/>的项
    /// </summary>
    public string? Include { get; set; }
}
