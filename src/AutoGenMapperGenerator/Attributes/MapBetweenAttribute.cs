using System;

namespace AutoGenMapperGenerator;

/// <summary>
/// 自定义映射关系
/// <para>定义在类上时，需要使用3个参数的构造函数，并且优先级高于定义在属性上的，如果存在单对多或者多对单的映射，建议定义在类上</para>
/// <para>定义在属性上，需要使用2个参数的构造函数，用于单对单的映射</para>
/// <para>定义在方法上，需要使用2个参数的构造函数</para>
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
public class MapBetweenAttribute : Attribute
{
    /// <summary>
    /// 目标类型
    /// </summary>
    private Type? TargetType { get; set; }
    /// <summary>
    /// 多对单的源属性
    /// </summary>
    private string[]? Sources { get; set; }
    /// <summary>
    /// 单对多的目标属性
    /// </summary>
    private string[]? Targets { get; set; }
    /// <summary>
    /// 单对单的源属性
    /// </summary>
    private string? Source { get; set; }
    /// <summary>
    /// 单对单的目标属性
    /// </summary>
    private string? Target { get; set; }

    /// <summary>
    /// 自定义映射方法，必须是静态方法
    /// <para>单对单</para>
    /// <code>
    /// <![CDATA[static TReturn {By}(TMember val)]]>
    /// </code>
    /// <para>多对单</para>
    /// <code>
    /// <![CDATA[static TReturn {By}(TMember1 val1, TMember2 val2, ...)]]>
    /// </code>
    /// <para>单对多</para>
    /// <code>
    /// <![CDATA[static (field1, field2, ...) {By}(TMember1 val)]]>
    /// </code>
    /// </summary>
    // <para>使用<see cref="MappingContext{TSource, TTarget}"/>作为参数</para>
    // <code>
    // <![CDATA[void {By}(MappingContext<TSource, TTarget> context)]]>
    // </code>
    public string? By { get; set; }

    /// <summary>
    /// (属性适用)自定义属性映射
    /// </summary>
    /// <param name="targetType">目标类型</param>
    /// <param name="target">目标类型的属性</param>
    public MapBetweenAttribute(Type targetType, string target)
    {
        TargetType = targetType;
        Target = target;
    }

    ///// <summary>
    ///// (属性和类适用)自定义属性映射
    ///// </summary>
    ///// <param name="targetType"></param>
    //public MapBetweenAttribute(Type targetType)
    //{
    //    TargetType = targetType;
    //}

    /// <summary>
    /// (类适用)自定义属性映射
    /// </summary>
    /// <param name="targetType"></param>
    /// <param name="source"></param>
    /// <param name="target"></param>
    public MapBetweenAttribute(Type targetType, string source, string target)
    {
        TargetType = targetType;
        Source = source;
        Target = target;
    }

    /// <summary>
    /// (类适用)自定义属性映射, 多对单
    /// </summary>
    /// <param name="targetType">目标类型</param>
    /// <param name="sources">自身的属性</param>
    /// <param name="target">目标类型的属性</param>
    public MapBetweenAttribute(Type targetType, string[] sources, string target)
    {
        TargetType = targetType;
        Sources = sources;
        Target = target;
    }

    /// <summary>
    /// (类适用)自定义属性映射, 单对多
    /// </summary>
    /// <param name="targetType">目标类型</param>
    /// <param name="source">自身的属性</param>
    /// <param name="targets">目标类型的属性</param>
    public MapBetweenAttribute(Type targetType, string source, string[] targets)
    {
        TargetType = targetType;
        Source = source;
        Targets = targets;
    }

    /// <summary>
    /// (方法适用)自定义属性映射
    /// </summary>
    /// <param name="source"></param>
    /// <param name="target"></param>
    public MapBetweenAttribute(string source, string target)
    {
        Source = source;
        Target = target;
    }

    /// <summary>
    /// (方法适用)自定义属性映射, 多对单
    /// </summary>
    /// <param name="sources"></param>
    /// <param name="target"></param>
    public MapBetweenAttribute(string[] sources, string target)
    {
        Sources = sources;
        Target = target;
    }

    /// <summary>
    /// (方法适用)自定义属性映射, 单对多
    /// </summary>
    /// <param name="source"></param>
    /// <param name="targets"></param>
    public MapBetweenAttribute(string source, string[] targets)
    {
        Source = source;
        Targets = targets;
    }
}
