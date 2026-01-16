using System;

namespace AutoGenMapperGenerator;

/// <summary>
/// 实现<see cref="IAutoMap"/>, 生成MapToXXX方法
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
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
