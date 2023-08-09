﻿using Palace.Server.Configuration;
using Palace.Server.Services;

using Microsoft.AspNetCore.Components;

namespace Palace.Server.Pages;

public partial class Login : ComponentBase
{
    [Inject] 
    NavigationManager? NavigationManager { get; set; } = default!;
    [Inject] 
    AdminLoginContext? AdminLoginContext { get; set; } = default!;
    [Inject]
	Configuration.GlobalSettings GlobalSettings { get; set; } = default!;

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
        AdminLoginContext!.AddToken(token);
        NavigationManager!.NavigateTo($"/?Token={token}", true);
    }

}