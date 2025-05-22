using System;

namespace AutoInjectGenerator;

/// <summary>
/// <para>注册自身, 等效于<see cref="AutoInjectAttribute"/>的<see cref="AutoInjectAttribute.ServiceType"/>为类型本身</para>
/// <para>当类型不实现接口时，等效[AutoInject]</para>
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class AutoInjectSelfAttribute : AutoInjectAttribute
{

}