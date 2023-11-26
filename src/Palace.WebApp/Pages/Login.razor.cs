using Palace.Server.Configuration;
using Palace.Server.Models;

namespace Palace.WebApp.Pages;

public partial class Login : ComponentBase
{
	[Inject]
	NavigationManager? NavigationManager { get; set; } = default!;
	[Inject]
	ILoginService? LoginService { get; set; } = default!;
	[Inject]
	Palace.Server.Configuration.GlobalSettings GlobalSettings { get; set; } = default!;

	LoginForm loginForm = new();
	CustomValidator loginValidator = new();

	public void Validate()
	{
		var errors = new Dictionary<string, List<string>>();
		if (GlobalSettings.AdminKey != loginForm.Key)
		{
			errors.Add(nameof(loginForm.Key), new List<string> { "invalid key" });
		}

		if (errors.Any())
		{
			loginValidator.DisplayErrors(errors);
			return;
		}
		var token = Guid.NewGuid();
		LoginService!.AddToken(token);
		NavigationManager!.NavigateTo($"/authenticate/{token}", true);
	}

}
