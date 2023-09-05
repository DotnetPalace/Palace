using Palace.Server.Services;

namespace Palace.Plugin._2FA;

internal class LoginService : ILoginService
{
	private readonly List<Guid> _tokenList;
	private readonly _2FAConfiguration _settings;

	public LoginService(_2FAConfiguration settings)
	{
		_tokenList = new List<Guid>();
		_settings = settings;
	}

	public Type LoginComponentType => typeof(Components.Login);
	public Type? MenuComponentType => typeof(Components.Menu);

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
		return new List<string>()
		{
			"admin",
		};
	}
}
