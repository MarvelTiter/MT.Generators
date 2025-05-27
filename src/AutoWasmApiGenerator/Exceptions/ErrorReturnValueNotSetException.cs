using System;
using System.Collections.Generic;
using System.Text;

namespace AutoWasmApiGenerator.Exceptions;

/// <summary>
/// <see cref="IExceptionResultFactory"/>未设置返回值
/// </summary>
/// <param name="message"></param>
public class ErrorReturnValueNotSetException(string message) : Exception (message)
{
}
