namespace AutoAopProxyGenerator;

/// <summary>
/// 执行状态
/// <para>NonExecute - 未执行</para>
/// <para>Break - 中断</para>
/// <para>Executed - 已执行</para>
/// </summary>
public enum ExecuteStatus
{
    /// <summary>
    /// 未执行
    /// </summary>
    NonExecute,
    /// <summary>
    /// 中断
    /// </summary>
    Break,
    /// <summary>
    /// 已执行
    /// </summary>
    Executed
}
