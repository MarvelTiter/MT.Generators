using System;
using System.Collections.Generic;

namespace AutoGenMapperGenerator;

/// <summary>
/// 生成MapToXXX方法
/// <para>
/// 默认自身的情况下，也可作为对自身的拷贝
/// </para>
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
/// 为指定类型创建
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public class GenMapperFromAttribute : Attribute
{
    /// <summary>
    /// 数据源类型
    /// </summary>
    public Type? SourceType { get; set; }

    /// <summary>
    /// 排除项
    /// </summary>
    public string[]? Exclude { get; set; }

    /// <summary>
    /// 自定义配置
    /// </summary>
    public Type? Configuration { get; set; }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="type"></param>
    /// <param name="exclude"></param>
    public GenMapperFromAttribute(Type type, params string[] exclude)
    {
        SourceType = type;
        Exclude = exclude;
    }

    /// <summary>
    /// 默认构造
    /// </summary>
    public GenMapperFromAttribute()
    {

    }
}

