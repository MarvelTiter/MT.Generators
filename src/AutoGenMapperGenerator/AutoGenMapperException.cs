using System;

namespace AutoGenMapperGenerator;

/// <summary>
/// 生成代码过程中的运行时异常
/// </summary>
public class AutoGenMapperException : Exception
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="message"></param>
    public AutoGenMapperException(string message) : base(message) { }
}