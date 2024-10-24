namespace AutoAopProxyGenerator;

/// <summary>
/// 执行状态
/// <para>NonExecute - 未执行</para>
/// <para>Breaked - 中断</para>
/// <para>Executed - 已执行</para>
/// <para>ExceptionOccurred - 发生异常</para>
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
    Breaked,
    /// <summary>
    /// 已执行
    /// </summary>
    Executed,
    /// <summary>
    /// 出现异常
    /// </summary>
    ExceptionOccurred
}
