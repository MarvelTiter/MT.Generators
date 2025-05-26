using AutoAopProxyGenerator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestProject1.AopGeneratorTest;

public interface IHello2
{
    void Hello(int? i);
    [AddAspectHandler(AspectType = typeof(MethodTestAop1))]
    [IgnoreAspect(typeof(LogAop))]
    Task<bool> RunJobAsync<T>() where T : LogAop;
}
public interface IHello<T> : IHello2
{
    void Hello(string? message);
    Task HelloAsync();
    Task HelloAsync(string message);
    //[AddAspectHandler(AspectType = typeof(ExceptionAop))]
    int Count(string message);
    Task<int> CountAsync();
    Task<int> CountAsync(string message);

    [AddAspectHandler(AspectType = typeof(MethodTestAop2))]
    [IgnoreAspect]
    Task RunJobAsync(string message);
}

[AddAspectHandler(AspectType = typeof(LogAop))]
[AddAspectHandler(AspectType = typeof(ExceptionAop))]
[AddAspectHandler(AspectType = typeof(MethodTestAop2), SelfOnly = true)]
public interface IWrapHello : IHello<int>
{
    //void Hello(string? message);
}

public interface IWrapHello2
{
    
}

[GenAspectProxy]
public class User : IWrapHello
{
    public void Hello(int? i)
    {
        Console.WriteLine("Hello world");
    }

    public Task HelloAsync()
    {
        throw new NotImplementedException();
    }
    public Task<int> CountAsync()
    {
        return Task.FromResult(3);
    }

    public Task<bool> RunJobAsync<T>() where T : LogAop
    {
        throw new NotImplementedException();
    }

    public void Hello(string? message)
    {
        throw new NotImplementedException();
    }

    public Task HelloAsync(string message)
    {
        throw new NotImplementedException();
    }

    public int Count(string message)
    {
        return message.Length;
    }

    public async Task<int> CountAsync(string message)
    {
        await Task.Delay(1);
        Console.WriteLine($"{message}: {message.Length}");
        return message.Length;
    }

    public Task RunJobAsync(string message)
    {
        throw new NotImplementedException();
    }
}

