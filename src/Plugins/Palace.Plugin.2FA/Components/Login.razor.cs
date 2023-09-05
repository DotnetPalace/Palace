using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;

using Palace.Plugin._2FA.Models;
using Palace.Server.Services;
using Palace.Shared;

namespace Palace.Plugin._2FA.Components;

public partial class Login
{
    [Inject]
    NavigationManager NavigationManager { get; set; } = default!;

    [Inject]
    _2FAConfiguration Settings { get; set; } = default!;

    [Inject]
    ILoginService LoginService { get; set; } = default!;    

    LoginForm loginForm = new();
    CustomValidator customValidator = new();
    string submitMessage = "Login";

    async Task Validate()
    {
        await Task.Yield();

        var errors = new Dictionary<string, List<string>>();
        if (loginForm.Step == "email")
        {
            if (loginForm.Email == Settings.DefaultAdminEmail)
            {
                submitMessage = "Connexion";
                loginForm.Step = "Digicode";
            }
        }
        else if (loginForm.Step == "digicode")
        {
            if (loginForm.Digicode == Settings.DefaultAdminKey)
            {
                var tokenId = Guid.NewGuid();
                LoginService.AddToken(tokenId);
                var pathWithToken = NavigationManager.GetUriWithQueryParameter("Token", tokenId);
                NavigationManager.NavigateTo(pathWithToken, true);
            }
        }

        if (errors.Any())
        {
            customValidator!.DisplayErrors(errors);
            return;
        }

        loginForm.Step = "digicode";
    }
}