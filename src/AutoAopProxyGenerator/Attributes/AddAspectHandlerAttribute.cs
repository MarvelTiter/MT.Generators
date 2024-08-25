using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoAopProxyGenerator;

/// <summary>
/// 标记需要生成代理类的类型
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class GenAspectProxyAttribute : Attribute
{

}

/// <summary>
/// 在类或者接口上配置切面处理
/// </summary>
[AttributeUsage(AttributeTargets.Interface)]
public sealed class AddAspectHandlerAttribute : Attribute
{
    public Type? AspectType { get; set; }
}

/// <summary>
/// 配置不需要切面的方法
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class IgnoreAspectAttribute : Attribute
{

}
