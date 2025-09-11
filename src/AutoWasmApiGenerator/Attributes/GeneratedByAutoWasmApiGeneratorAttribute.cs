using System;
using System.Collections.Generic;
using System.Text;

namespace AutoWasmApiGenerator.Attributes;

/// <summary>
/// 
/// </summary>
public enum PartType
{
    /// <summary>
    /// 控制器
    /// </summary>
    Controller,
    /// <summary>
    /// 调用类
    /// </summary>
    ApiInvoker
}

/// <summary>
/// 作为生成器生成的接口调用类的标识
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class GeneratedByAutoWasmApiGeneratorAttribute : Attribute
{
    /// <summary>
    /// 接口类型
    /// </summary>
    public Type? InterfaceType { get; set; }
    /// <summary>
    /// 生成的部分
    /// </summary>
    public PartType Part { get; set; }
}
