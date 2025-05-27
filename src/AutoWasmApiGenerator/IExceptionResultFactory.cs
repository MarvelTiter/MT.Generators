using AutoWasmApiGenerator.Exceptions;
using System;
using System.Collections.Concurrent;

namespace AutoWasmApiGenerator;

/// <summary>
/// 
/// </summary>
public interface IExceptionResultFactory
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TReturn"></typeparam>
    /// <param name="context"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    bool GetErrorResult<TReturn>(ExceptionContext context, out TReturn value);

}
/// <summary>
/// 配置异常返回值
/// </summary>
public interface IExceptionResultConfigurator
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TResult"></typeparam>
    /// <param name="action"></param>
    /// <returns></returns>
    IExceptionResultConfigurator CreateErrorResult<TResult>(Func<ExceptionContext, object> action);
}

internal class ExceptionResultFactory : IExceptionResultFactory, IExceptionResultConfigurator
{
    private readonly ConcurrentDictionary<Type, Func<ExceptionContext, object>> errors = [];
    public IExceptionResultConfigurator CreateErrorResult<TResult>(Func<ExceptionContext, object> action)
    {
        errors.GetOrAdd(typeof(TResult), action);
        return this;
    }

    public bool GetErrorResult<TReturn>(ExceptionContext context, out TReturn value)
    {
        //if (errors.TryGetValue(typeof(TReturn), out var action))
        //{
        //    value = (TReturn)action.Invoke(context);
        //    return true;
        //}
        var targetType = typeof(TReturn);
        foreach (var item in errors)
        {
            if (item.Key == targetType
                || item.Key.IsAssignableFrom(targetType)
                )
            {
                value = (TReturn)item.Value.Invoke(context);
                return true;
            }
        }
        value = default!;
        return false;
    }
}