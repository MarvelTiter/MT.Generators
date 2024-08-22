using System;

namespace AutoGenMapperGenerator;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public class GenMapperAttribute : Attribute
{
    private readonly string[]? values;
    public Type? TargetType { get; set; }

    public GenMapperAttribute()
    {

    }

    public GenMapperAttribute(Type targetType)
    {
        TargetType = targetType;
    }

    public GenMapperAttribute(Type targetType, params string[] values)
    {
        this.values = values;
        TargetType = targetType;
    }
}
[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public class MapIgnore : Attribute
{

}
[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = true)]
public class MapToAttribute : Attribute
{
    /// <summary>
    /// 指定转换来源
    /// </summary>
    public Type? Target { get; set; }
    /// <summary>
    /// 指定转换来源属性
    /// </summary>
    public string? Name { get; set; }
    /// <summary>
    /// <para>指定转换处理方法，当指定了<see cref="Target"/>时，方法签名应该为<code>void {By}(Target target)</code>否则应该为<code>void {By}()</code></para>
    /// <para>不能为静态方法</para>
    /// </summary>
    public string? By { get; set; }
}

/// <summary>
/// <para>自定义转换</para>
/// <para>相同的Source只有第一个生效</para>
/// </summary>
[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = true)]
public class MapFromAttribute : Attribute
{
    /// <summary>
    /// 指定转换来源
    /// </summary>
    public Type? Source { get; set; }
    /// <summary>
    /// 指定转换来源属性
    /// </summary>
    public string? Name { get; set; }
    /// <summary>
    /// <para>指定转换处理方法，当指定了<see cref="Source"/>时，方法签名应该为<code>TMember {By}(Target target)</code>否则应该为<code>TMember {By}()</code></para>
    /// <para>可以为静态方法</para>
    /// </summary>
    public string? By { get; set; }
}