using System;
using System.Collections.Generic;
using System.Text;

namespace AutoInjectGenerator;

/// <summary>
/// 注册生命周期
/// <para>Transient</para>
/// <para>Scoped</para>
/// <para>Singleton</para>
/// </summary>
public enum InjectLifeTime
{
    /// <summary>
    /// 
    /// </summary>
    Singleton,
    /// <summary>
    /// 
    /// </summary>
    Scoped,
    /// <summary>
    /// 
    /// </summary>
    Transient,
}
