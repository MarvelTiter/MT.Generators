using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using AutoWasmApiGenerator;
using Blazor.Test.Client.Models;
using AutoPageStateContainerGenerator;
using Blazor.Test.Client.Aop;
[assembly: ApiClientAssembly]
var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.Services.ConfigureHttpClientDefaults(c =>
{
    c.ConfigureHttpClient(h => { h.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress); });
});

//builder.Services.AddScoped<IHelloService, HelloServiceApiInvoker>();
//builder.Services.AddScoped<IB, BApiClient>();
builder.Services.AddScoped<IGeneratedApiClientDelegatingHandler, HttpExceptionHandler>();
builder.Services.AddStateContainers();
//builder.Services.AddGeneratedContainerServices();
builder.Services.AddGeneratedApiClientServices();
builder.Services.AddAutoWasmErrorResultHandler(config =>
{
    config.CreateErrorResult<QueryResult>(context =>
    {
        return new QueryResult() { IsSuccess = false, Message = context.Exception.Message };
    });
});
await builder.Build().RunAsync();
