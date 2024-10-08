using System;
using System.Collections.Generic;

namespace AutoGenMapperGenerator;

/// <summary>
/// 生成MapToXXX方法
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public class GenMapperAttribute : Attribute
{
    private readonly string[]? values;
    /// <summary>
    /// 目标类型，默认是自身
    /// </summary>
    public Type? TargetType { get; set; }

    /// <summary>
    /// 默认构造
    /// </summary>
    public GenMapperAttribute()
    {

    }

    /// <summary>
    /// 指定目标类型
    /// </summary>
    /// <param name="targetType">目标类型</param>
    public GenMapperAttribute(Type targetType)
    {
        TargetType = targetType;
    }

    /// <summary>
    /// 指定目标类型，并且指定构造函数参数
    /// </summary>
    /// <param name="targetType">目标类型</param>
    /// <param name="values">构造参数</param>
    public GenMapperAttribute(Type targetType, params string[] values)
    {
        this.values = values;
        TargetType = targetType;
    }
}

/// <summary>
/// 忽略映射
/// </summary>
[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
public class MapIgnoreAttribute : Attribute
{

}

/// <summary>
/// 
/// </summary>
[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = true)]
public class MapToAttribute : Attribute
{
    /// <summary>
    /// 指定目标类型
    /// </summary>
    public Type? Target { get; set; }
    /// <summary>
    /// 指定目标类型属性
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

/// <summary>
/// 为<see cref="IDictionary{String,Object}"/> 生成MapTo拓展方法
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public class GenMapperFromDictionaryAttribute : Attribute
{
    private readonly string[]? values;
    /// <summary>
    /// 指定构造函数参数
    /// </summary>
    /// <param name="values"></param>
    public GenMapperFromDictionaryAttribute(params string[] values)
    {
        this.values = values;
    }
}

/// <summary>
/// 
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class StaticMapperContextAttribute : Attribute { }
