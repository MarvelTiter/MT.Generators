using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoAopProxyGenerator;

/// <summary>
/// 在接口/方法上配置切面处理类型<see cref="AspectType"/> = <see cref="IAspectHandler"/>，默认会包含继承而来的方法，可使用<see cref="SelfOnly"/>设置
/// </summary>
[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Method, AllowMultiple = true)]
public sealed class AddAspectHandlerAttribute : Attribute
{
    /// <summary>
    /// 切面处理类型
    /// </summary>
    public Type? AspectType { get; set; }
    /// <summary>
    /// 是否只适用于当前类型的方法，当设置为true时，不适用于继承而来的方法
    /// </summary>
    public bool SelfOnly { get; set; }
}
