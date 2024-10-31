using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json;

namespace Blazor.Test.Controllers;

[Route("[controller]")]
public class AccountController : ControllerBase
{
    [HttpGet("[action]")]
    public async Task<IActionResult> Login()
    {
        // 初始化一个声明列表，包含用户的必要信息
        var claims = new List<Claim>
    {
        new(ClaimTypes.Name, "Admin"),
    };

        // 返回一个新的ClaimsPrincipal对象，包含所有声明
        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
        await HttpContext.SignInAsync(principal, new()
        {
            ExpiresUtc = DateTime.UtcNow.AddDays(15),
            AllowRefresh = true
        });
        return Ok();
    }
}

[Route("[controller]")]
public class TestController : ControllerBase
{
    [HttpGet("[action]")]
    public IActionResult Hello()
    {
        var request = new HttpRequestMessage();

        //request.Headers.Add()
        return Ok();
    }
}
