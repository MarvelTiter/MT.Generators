using Blazor.Test.Client.Services;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
[assembly:AutoWasmApiGenerator.ApiInvokerAssembly]
var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.Services.ConfigureHttpClientDefaults(c =>
{
    c.ConfigureHttpClient(h => { h.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress); });
});

builder.Services.AddScoped<IHelloService, HelloServiceApiInvoker>();

await builder.Build().RunAsync();
