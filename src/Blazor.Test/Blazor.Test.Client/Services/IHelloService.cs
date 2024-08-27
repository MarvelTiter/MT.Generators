using AutoAopProxyGenerator;
using Blazor.Test.Client.Aop;

namespace Blazor.Test.Client.Services;

[AddAspectHandler(AspectType = typeof(ExceptionAop))]
[AddAspectHandler(AspectType = typeof(TestAop))]
public interface IHelloService
{
    Task<string> SayHelloAsync(string name);
    [IgnoreAspect]
    string SayHelloDirectly(string name);
}

[GenAspectProxy]
public class HelloService : IHelloService
{
    public async Task<string> SayHelloAsync(string name)
    {
        if (name.IndexOf("M")  > -1)
        {
            throw new Exception("Name Error");
        }
        await Task.Delay(500);
        return $"Hello, {name}!";
    }

    public string SayHelloDirectly(string name)
    {
        if (name.IndexOf("M") > -1)
        {
            throw new Exception("Name Error");
        }
        return $"Hello, {name}!";
    }
}
