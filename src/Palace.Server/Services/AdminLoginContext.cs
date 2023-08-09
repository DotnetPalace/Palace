﻿using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;

namespace Palace.Server.Services;

public class AdminLoginContext
{
    private readonly List<Guid> _tokenList;

    public AdminLoginContext()
    {
        _tokenList = new List<Guid>();
    }

    public void AddToken(Guid token)
    {
        if (_tokenList.Contains(token))
        {
            return;
        }
        _tokenList.Add(token);
    }

    public bool Contains(Guid token)
    {
        return _tokenList.Contains(token);
    }

    public void Remove(Guid token)
    {
        _tokenList.Remove(token);
    }

    public async Task TrySignIn(HttpContext httpContext, ClaimsPrincipal claimsPrincipal)
    {
        if (claimsPrincipal.Identity!.IsAuthenticated)
        {
            return;
        }
        var tokenParam = $"{httpContext!.Request.Query["Token"]}";
        if (string.IsNullOrWhiteSpace(tokenParam))
        {
            return;
        }

        Guid.TryParse(tokenParam, out Guid token);
        if (token != Guid.Empty && Contains(token))
        {
            Remove(token);
            var claims = new List<Claim>();
            claims.Add(new Claim(ClaimTypes.Role, "admin"));

            var claimsIdentity = new ClaimsIdentity(
                claims, CookieAuthenticationDefaults.AuthenticationScheme);

            var userPrincipal = new ClaimsPrincipal(claimsIdentity);

            var authProperties = new AuthenticationProperties
            {
                AllowRefresh = true,
                IsPersistent = true,
            };

            await httpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                userPrincipal,
                authProperties);
        }
    }

}
