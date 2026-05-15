using InjectTest;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Blazor.Test.Client.Services;

public static class HelloServiceEndPoints
{
    //[global::System.CodeDom.Compiler.GeneratedCode("AutoWasmApiGenerator.ControllerGenerator", "2026.2.12.1")]
    //public static void MapHelloServiceEndPoints(this WebApplication app)
    //{
    //    var group = app.MapGroup("api/hello/test");
    //    group.MapPost("SayHello", ([global::Microsoft.AspNetCore.Mvc.FromQuery] string name, Blazor.Test.Client.Services.IHelloService service) => service.SayHelloAsync(name));

    //    group.MapGet("TestReturnTuple", TestReturnTuple);
        
    //}

    //public static async global::System.Threading.Tasks.Task<string> TestReturnTuple([global::Microsoft.AspNetCore.Mvc.FromQuery] string name, IHelloService service)
    //{
    //    var _return_gen = await service.TestReturnTuple(name);
    //    var _anonymous_gen = new { Success = _return_gen.Success, Message = _return_gen.Message, Info = new { Prop = _return_gen.Info.Prop, Value = _return_gen.Info.Value, } };
    //    return global::System.Text.Json.JsonSerializer.Serialize(_anonymous_gen, AutoWasmApiGenerator.AutoWasmApiGeneratorJsonHelper.TupleOption);
    //}

    //[global::Microsoft.AspNetCore.Mvc.HttpPost("TestHeaderParameter")]
    //[global::System.CodeDom.Compiler.GeneratedCode("AutoWasmApiGenerator.ControllerGenerator", "2026.2.12.1")]
    //public System.Threading.Tasks.Task<string> TestHeaderParameter([global::Microsoft.AspNetCore.Mvc.FromHeader] string name)
    //  => _proxyService_gen.TestHeaderParameter(name);

    //[global::Microsoft.AspNetCore.Mvc.HttpPost("TestQueryParameter")]
    //[global::System.CodeDom.Compiler.GeneratedCode("AutoWasmApiGenerator.ControllerGenerator", "2026.2.12.1")]
    //public System.Threading.Tasks.Task<string> TestQueryParameter([global::Microsoft.AspNetCore.Mvc.FromQuery] string name)
    //  => _proxyService_gen.TestQueryParameter(name);

    //[global::Microsoft.AspNetCore.Mvc.HttpPost("TestQueryParameter2")]
    //[global::System.CodeDom.Compiler.GeneratedCode("AutoWasmApiGenerator.ControllerGenerator", "2026.2.12.1")]
    //public System.Threading.Tasks.Task<string> TestQueryParameter2([global::Microsoft.AspNetCore.Mvc.FromQuery] string name, [global::Microsoft.AspNetCore.Mvc.FromQuery] int age)
    //  => _proxyService_gen.TestQueryParameter2(name, age);

    //[global::Microsoft.AspNetCore.Mvc.HttpPost("TestReturnQueryResultInt")]
    //[global::System.CodeDom.Compiler.GeneratedCode("AutoWasmApiGenerator.ControllerGenerator", "2026.2.12.1")]
    //public System.Threading.Tasks.Task<Blazor.Test.Client.Models.QueryResult<int>> TestReturnQueryResultInt([global::Microsoft.AspNetCore.Mvc.FromQuery] string name)
    //  => _proxyService_gen.TestReturnQueryResultInt(name);

    //[global::Microsoft.AspNetCore.Mvc.HttpPost("TestReturnQueryResult")]
    //[global::System.CodeDom.Compiler.GeneratedCode("AutoWasmApiGenerator.ControllerGenerator", "2026.2.12.1")]
    //public System.Threading.Tasks.Task<Blazor.Test.Client.Models.QueryResult> TestReturnQueryResult([global::Microsoft.AspNetCore.Mvc.FromQuery] string name)
    //  => _proxyService_gen.TestReturnQueryResult(name);

    //[global::Microsoft.AspNetCore.Mvc.HttpGet("TestReturnTuple")]
    //[global::System.CodeDom.Compiler.GeneratedCode("AutoWasmApiGenerator.ControllerGenerator", "2026.2.12.1")]
    //public async global::System.Threading.Tasks.Task<string> TestReturnTuple([global::Microsoft.AspNetCore.Mvc.FromQuery] string name)
    //{
    //    var _return_gen = await _proxyService_gen.TestReturnTuple(name);
    //    var _anonymous_gen = new { Success = _return_gen.Success, Message = _return_gen.Message, Info = new { Prop = _return_gen.Info.Prop, Value = _return_gen.Info.Value, } };
    //    return global::System.Text.Json.JsonSerializer.Serialize(_anonymous_gen, AutoWasmApiGenerator.AutoWasmApiGeneratorJsonHelper.TupleOption);
    //}

    //[global::Microsoft.AspNetCore.Mvc.HttpGet("TestReturnVoid")]
    //[global::System.CodeDom.Compiler.GeneratedCode("AutoWasmApiGenerator.ControllerGenerator", "2026.2.12.1")]
    //public void TestReturnVoid()
    //  => _proxyService_gen.TestReturnVoid();

    //[global::Microsoft.AspNetCore.Mvc.HttpPost("TestRouterParameter")]
    //[global::System.CodeDom.Compiler.GeneratedCode("AutoWasmApiGenerator.ControllerGenerator", "2026.2.12.1")]
    //public System.Threading.Tasks.Task<string> TestRouterParameter([global::Microsoft.AspNetCore.Mvc.FromQuery] string test)
    //  => _proxyService_gen.TestRouterParameter(test);

    //[global::Microsoft.AspNetCore.Mvc.HttpPost("TestFormParameter")]
    //[global::System.CodeDom.Compiler.GeneratedCode("AutoWasmApiGenerator.ControllerGenerator", "2026.2.12.1")]
    //public System.Threading.Tasks.Task<string> TestFormParameter([global::Microsoft.AspNetCore.Mvc.FromForm] string name)
    //  => _proxyService_gen.TestFormParameter(name);

    //[global::Microsoft.AspNetCore.Mvc.HttpPost("TestMultiParameter/{id}")]
    //[global::System.CodeDom.Compiler.GeneratedCode("AutoWasmApiGenerator.ControllerGenerator", "2026.2.12.1")]
    //public System.Threading.Tasks.Task<string> TestMultiParameter([global::Microsoft.AspNetCore.Mvc.FromRoute] int id, [global::Microsoft.AspNetCore.Mvc.FromQuery] string name)
    //  => _proxyService_gen.TestMultiParameter(id, name);

    //[global::Microsoft.AspNetCore.Mvc.HttpPost("TestQueryAndBodyParameter")]
    //[global::System.CodeDom.Compiler.GeneratedCode("AutoWasmApiGenerator.ControllerGenerator", "2026.2.12.1")]
    //public System.Threading.Tasks.Task<string> TestQueryAndBodyParameter([global::Microsoft.AspNetCore.Mvc.FromQuery] int id, [global::Microsoft.AspNetCore.Mvc.FromBody] Blazor.Test.Client.Services.RequestTest body)
    //  => _proxyService_gen.TestQueryAndBodyParameter(id, body);

}