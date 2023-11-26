using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.AspNetCore.Authorization;

using Microsoft.AspNetCore.Components.Forms;
using Palace.WebApp.Services;

namespace Palace.WebApp.Controllers;

public class AuthenticateController : Controller
{
    private readonly ILogger<AuthenticateController> _logger;
    private readonly ILoginService _loginService;

    public AuthenticateController(
        ILogger<AuthenticateController> logger,
        ILoginService loginService)
    {
        _logger = logger;
        _loginService = loginService;
    }

    [AllowAnonymous]
    [HttpGet]
    [Microsoft.AspNetCore.Mvc.Route("/authenticate/{token:guid}")]
    public async Task<IActionResult> Authenticate(Guid token)
    {
        if (User.Identity!.IsAuthenticated)
        {
            return Redirect("/");
        }

        if (_loginService.Contains(token))
        {
            _loginService.Remove(token);
            var claims = new List<Claim>();
            claims.Add(new Claim(ClaimTypes.Name , "admin"));
            var roleList = _loginService.GetRoleList(token);
            foreach (var role in roleList)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var claimsIdentity = new ClaimsIdentity(
                claims, CookieAuthenticationDefaults.AuthenticationScheme);

            var userPrincipal = new ClaimsPrincipal(claimsIdentity);

            var authProperties = new AuthenticationProperties
            {
                AllowRefresh = true,
                IsPersistent = true,
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                userPrincipal,
                authProperties);
        }

        return Redirect("/");
    }

    [HttpGet]
    [Microsoft.AspNetCore.Mvc.Route("/logout")]
    public async Task<IActionResult> TryLogout()
    {
        if (User.Identity == null
            || !User.Identity.IsAuthenticated)
        {
            return Redirect("/");
        }
        await HttpContext.SignOutAsync();

        _logger.LogInformation("User {name} logged out", User.Identity.Name);

        return Redirect("/");
    }
}
