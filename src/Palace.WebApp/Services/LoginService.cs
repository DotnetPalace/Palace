using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Palace.Server.Services;
using System.Security.Claims;

namespace Palace.WebApp.Services;

public class LoginService : ILoginService
{
    private readonly List<Guid> _tokenList;

    public LoginService()
    {
        _tokenList = new List<Guid>();
    }

    public Type LoginComponentType => typeof(Pages.Login);
    public Type? MenuComponentType => null;

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

    public List<string> GetRoleList(Guid token)
    {
        return new();
    }

}
