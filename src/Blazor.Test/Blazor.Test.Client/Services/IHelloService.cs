using AutoAopProxyGenerator;
using AutoWasmApiGenerator;
using Blazor.Test.Client.Aop;

namespace Blazor.Test.Client.Services;

public class RequestTest
{
    public string? Value { get; set; }
}

[AddAspectHandler(AspectType = typeof(ExceptionAop))]
[AddAspectHandler(AspectType = typeof(TestAop))]
[WebController(Route = "hello/test", Authorize = true)]
[ApiInvokerGenerate]
public interface IHelloService
{
    Task<string> SayHelloAsync(string name);

    Task<int> TestHeaderParameter([WebMethodParameterBinding(BindingType.FromHeader)] string name);
    Task<int> TestQueryParameter([WebMethodParameterBinding(BindingType.FromQuery)] string name);

    //[WebMethod(Route = "rp")]
    Task<int> TestRouterParameter(string test);
    Task<int> TestFormParameter([WebMethodParameterBinding(BindingType.FromForm)] string name);
    [WebMethod(Route = "{id}")]
    Task<string> TestMultiParameter([WebMethodParameterBinding(BindingType.FromRoute)] int id
        , [WebMethodParameterBinding(BindingType.FromQuery)] string name);
    Task<string> TestQueryAndBodyParameter([WebMethodParameterBinding(BindingType.FromQuery)] int id
        , [WebMethodParameterBinding(BindingType.FromBody)] RequestTest body);
}

[GenAspectProxy]
public class HelloService : IHelloService
{
    public async Task<int> TestHeaderParameter(string name)
    {
        await Task.Delay(500);
        return name.Length;
    }

    public async Task<string> SayHelloAsync(string name)
    {
        await Task.Delay(500);
        return $"Hello, {name} !";
    }

    public Task<int> TestQueryParameter([WebMethodParameterBinding(BindingType.FromQuery)] string name)
    {
        return Task.FromResult(name.Length);
    }

    public Task<int> TestRouterParameter([WebMethodParameterBinding(BindingType.FromRoute)] string name)
    {
        return Task.FromResult(name.Length);
    }

    public Task<int> TestFormParameter([WebMethodParameterBinding(BindingType.FromForm)] string name)
    {
        return Task.FromResult(name.Length);
    }

    public Task<string> TestMultiParameter([WebMethodParameterBinding(BindingType.FromRoute)] int id, [WebMethodParameterBinding(BindingType.FromQuery)] string name)
    {
        return Task.FromResult($"id: {id}, name: {name}");
    }

    public Task<string> TestQueryAndBodyParameter([WebMethodParameterBinding(BindingType.FromQuery)] int id, [WebMethodParameterBinding(BindingType.FromBody)] RequestTest body)
    {
        return Task.FromResult($"id: {id}, name: {body.Value}");
    }
}
