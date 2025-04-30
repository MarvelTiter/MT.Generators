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
[Obsolete("使用MapBetweenAttribute代替", true)]
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
    /// <para>如果是静态方法，需要将属性值作为参数传递</para>
    /// <code>void {By}(Target target, TMember value)</code>
    /// </summary>
    public string? By { get; set; }
}

/// <summary>
/// <para>自定义转换</para>
/// <para>相同的Source只有第一个生效</para>
/// </summary>
[AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = true)]
[Obsolete("使用MapBetweenAttribute代替", true)]
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
/// 自定义映射关系
/// <para>定义在类上时，需要使用3个参数的构造函数，并且优先级高于定义在属性上的，如果存在单对多或者多对单的映射，建议定义在类上</para>
/// <para>定义在属性上，需要使用2个参数的构造函数，用于单对单的映射</para>
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property, Inherited = false, AllowMultiple = true)]
public class MapBetweenAttribute : Attribute
{
    private readonly Type? targetType;
    private readonly string[]? sources;
    private readonly string[]? targets;
    private readonly string? source;
    private readonly string? target;

    /// <summary>
    /// 自定义映射方法
    /// <para>单对单</para>
    /// <code>
    /// <![CDATA[TReturn {By}(TMember val)]]>
    /// </code>
    /// <para>多对单</para>
    /// <code>
    /// <![CDATA[TReturn {By}(TMember1 val1, TMember2 val2, ...)]]>
    /// </code>
    /// <para>单对多</para>
    /// <code>
    /// <![CDATA[object[] {By}(TMember1 val)]]>
    /// </code>
    /// <para>使用<see cref="MappingContext{TSource, TTarget}"/>作为参数</para>
    /// <code>
    /// <![CDATA[void {By}(MappingContext<TSource, TTarget> context)]]>
    /// </code>
    /// </summary>
    public string? By { get; set; }

    /// <summary>
    /// 自定义属性映射
    /// </summary>
    /// <param name="targetType"></param>
    public MapBetweenAttribute(Type targetType)
    {
        this.targetType = targetType;
    }
    
    /// <summary>
    /// 自定义属性映射
    /// </summary>
    /// <param name="targetType">目标类型</param>
    /// <param name="target">目标类型的属性</param>
    public MapBetweenAttribute(Type targetType, string target)
    {
        this.targetType = targetType;
        this.target = target;
    }
        
    /// <summary>
    /// 自定义属性映射
    /// </summary>
    /// <param name="targetType"></param>
    /// <param name="source"></param>
    /// <param name="target"></param>
    public MapBetweenAttribute(Type targetType, string source, string target)
    {
        this.targetType = targetType;
        this.source = source;
        this.target = target;
    }

    /// <summary>
    /// 指定多对单
    /// </summary>
    /// <param name="targetType">目标类型</param>
    /// <param name="sources">自身的属性</param>
    /// <param name="target">目标类型的属性</param>
    public MapBetweenAttribute(Type targetType, string[] sources, string target)
    {
        this.targetType = targetType;
        this.sources = sources;
        this.target = target;
    }

    /// <summary>
    /// 指定单对多
    /// </summary>
    /// <param name="targetType">目标类型</param>
    /// <param name="source">自身的属性</param>
    /// <param name="targets">目标类型的属性</param>
    public MapBetweenAttribute(Type targetType, string source, string[] targets)
    {
        this.targetType = targetType;
        this.source = source;
        this.targets = targets;
    }
}
