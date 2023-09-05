using System.Security.Claims;

namespace Palace.Server.Services;

public interface ILoginService
{
    void AddToken(Guid token);
    bool Contains(Guid token);
    void Remove(Guid token);
    List<string> GetRoleList(Guid token);
    Type LoginComponentType { get; }
    Type? MenuComponentType { get; }
}