using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;

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
    public ActionResult<(string, int)> Hello()
    {
        return Ok((Name: "hello", Age: 123));
    }

    [HttpGet("[action]")]
    public ActionResult<Task<string>> Hello2()
    {
        return Ok(StringResult());
    }

    [HttpGet("[action]")]
    public async Task<ActionResult<Task<string>>> Hello3()
    {
        var r = await StringResult();
        return Ok(r);
    }

    [HttpGet("[action]")]
    public void Command([FromQuery] (string, int) p)
    {
        Console.WriteLine($"Command {p}");
    }

    Task<string> StringResult() => Task.FromResult("Hello2");

    object ParseJsonToTuple(string json)
    {
        var doc = JsonDocument.Parse(json);
        var je = doc.RootElement;
        je.GetProperty("").GetDateTime();
        return 0;
    }
}
