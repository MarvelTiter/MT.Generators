using AutoAopProxyGenerator;
using AutoWasmApiGenerator;
using Blazor.Test.Client.Aop;
using Blazor.Test.Client.Models;

namespace Blazor.Test.Client.Services;

public class RequestTest
{
    public string? Value { get; set; }
}

[AddAspectHandler(AspectType = typeof(ExceptionAop))]
[AddAspectHandler(AspectType = typeof(TestAop))]
[WebController(Route = "hello/test")]
public interface IHelloService
{
    Task<string> SayHelloAsync(string name);
    Task<string> TestHeaderParameter([WebMethodParameterBinding(BindingType.FromHeader)] string name);
    Task<string> TestQueryParameter([WebMethodParameterBinding(BindingType.FromQuery)] string name);
    Task<string> TestQueryParameter2(string name, int age);

    Task<QueryResult<int>> TestReturnQueryResultInt(string name);
    Task<QueryResult> TestReturnQueryResult(string name);

    [WebMethod(Method = WebMethod.Get)]
    //[ApiInvokeNotSupported]
    Task<(bool Success, string Message, (string Prop, int Value) Info)> TestReturnTuple(string name);

    [WebMethod(Method = WebMethod.Get)]
    [ApiInvokeNotSupported]
    void TestReturnVoid();

    //[WebMethod(Route = "rp")]
    Task<string> TestRouterParameter(string test);
    Task<string> TestFormParameter([WebMethodParameterBinding(BindingType.FromForm)] string name);

    [WebMethod(Route = "{id}")]
    Task<string> TestMultiParameter([WebMethodParameterBinding(BindingType.FromRoute)] int id
        , [WebMethodParameterBinding(BindingType.FromQuery)] string name);

    Task<string> TestQueryAndBodyParameter([WebMethodParameterBinding(BindingType.FromQuery)] int id
        , [WebMethodParameterBinding(BindingType.FromBody)] RequestTest body);
}

//[GenAspectProxy]
public class HelloService : IHelloService
{
    public async Task<string> TestHeaderParameter(string name)
    {
        await Task.Delay(500);
        return $"{name} {name.Length}";
    }

    public async Task<string> SayHelloAsync(string name)
    {
        await Task.Delay(500);
        return $"Hello, {name} !";
    }

    public Task<string> TestQueryParameter([WebMethodParameterBinding(BindingType.FromQuery)] string name)
    {
        return Task.FromResult($"{name} {name.Length}");
    }

    public Task<string> TestQueryParameter2(string name, int age)
    {
        return Task.FromResult($"{name} {name.Length} age: {age}");
    }

    public Task<QueryResult<int>> TestReturnQueryResultInt(string name)
    {
        return QueryResult.Return<int>(true).SetPayload(name.Length).AsTask();
    }

    public Task<QueryResult> TestReturnQueryResult(string name)
    {
        return QueryResult.Success().SetPayload(name).AsTask();
    }

    public Task<(bool, string, (string, int))> TestReturnTuple(string name)
    {
        return Task.FromResult((true, name, (name, name.Length)));
    }

    public Task<string> TestRouterParameter([WebMethodParameterBinding(BindingType.FromRoute)] string name)
    {
        return Task.FromResult(name);
    }

    public Task<string> TestFormParameter([WebMethodParameterBinding(BindingType.FromForm)] string name)
    {
        return Task.FromResult($"{name} {name.Length}");
    }

    public Task<string> TestMultiParameter([WebMethodParameterBinding(BindingType.FromRoute)] int id, [WebMethodParameterBinding(BindingType.FromQuery)] string name)
    {
        return Task.FromResult($"id: {id}, name: {name}");
    }

    public Task<string> TestQueryAndBodyParameter([WebMethodParameterBinding(BindingType.FromQuery)] int id, [WebMethodParameterBinding(BindingType.FromBody)] RequestTest body)
    {
        return Task.FromResult($"id: {id}, name: {body.Value}");
    }

    public void TestReturnVoid()
    {
        throw new NotImplementedException();
    }
}