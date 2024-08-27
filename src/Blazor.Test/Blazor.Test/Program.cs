using Blazor.Test.Client.Aop;
using Blazor.Test.Client.Pages;
using Blazor.Test.Client.Services;
using Blazor.Test.Components;

[assembly:AutoWasmApiGenerator.WebControllerAssembly]
[assembly:AutoWasmApiGenerator.ApiInvokerAssembly]
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

builder.Services.AddScoped<IHelloService, HelloService>();
builder.Services.AddScoped<TestAop>();
builder.Services.AddScoped<ExceptionAop>();
builder.Host.UseServiceProviderFactory(new AutoAopProxyGenerator.AutoAopProxyServiceProviderFactory());


var app = builder.Build();



// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Blazor.Test.Client._Imports).Assembly);

app.Run();
