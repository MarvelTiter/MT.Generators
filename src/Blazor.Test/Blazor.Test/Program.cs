using Blazor.Test;
using Blazor.Test.Client.Aop;
using Blazor.Test.Client.Pages;
using Blazor.Test.Client.Services;
using Blazor.Test.Components;
using Microsoft.AspNetCore.Authentication.Cookies;


[assembly:AutoWasmApiGenerator.WebControllerAssembly]
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

builder.Services.AddScoped<IHelloService, HelloService>();
builder.Services.AddScoped<TestAop>();
builder.Services.AddScoped<ExceptionAop>();
builder.Host.UseServiceProviderFactory(new AutoAopProxyGenerator.AutoAopProxyServiceProviderFactory());
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie();
builder.Services.AddControllers();
builder.Services.Inject();
builder.Services.AddHttpClient();
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
app.UseAuthorization();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Blazor.Test.Client._Imports).Assembly);
app.MapControllers();
app.Run();
