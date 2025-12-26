using System;

namespace AutoGenMapperGenerator;

/// <summary>
/// 指定构造函数参数
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class MapConstructorAttribute : Attribute
{
    private Type? targetType;
    private string[] parameters;
    /// <summary>
    /// 用在类上的
    /// </summary>
    /// <param name="targetType"></param>
    /// <param name="parameters"></param>
    public MapConstructorAttribute(Type targetType, params string[] parameters)
    {
        this.targetType = targetType;
        this.parameters = parameters;
    }

    /// <summary>
    /// 用在方法上的
    /// </summary>
    /// <param name="parameters"></param>
    public MapConstructorAttribute(params string[] parameters)
    {
        this.parameters = parameters;
    }
}