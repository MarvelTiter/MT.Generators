using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoAopProxyGenerator;

/// <summary>
/// 在类或者接口上配置切面处理
/// </summary>
[AttributeUsage(AttributeTargets.Interface, AllowMultiple = true)]
public sealed class AddAspectHandlerAttribute : Attribute
{
    public Type? AspectType { get; set; }
}
