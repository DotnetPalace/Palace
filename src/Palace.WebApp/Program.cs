using LogRWebMonitor;

using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Palace.Server;
using Palace.Server.Pages;
using Palace.WebApp.Services;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Palace.Tests")]

var builder = WebApplication.CreateBuilder(args);

var settings = await builder.AddPalaceServer();

builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents();

builder.Services.AddControllers();

builder.Services.AddScoped<DialogService>();
builder.Services.TryAddSingleton<ILoginService, LoginService>();
builder.Services.AddScoped<ClipboardService>();

builder.Services.AddDataProtection()
        .SetApplicationName(settings.ApplicationName)
        .AddKeyManagementOptions(options =>
        {
            options.AutoGenerateKeys = true;
        })
        .PersistKeysToFileSystem(new DirectoryInfo(settings.DataFolder))
        .SetDefaultKeyLifetime(TimeSpan.FromDays(400));

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
        .AddCookie(options =>
        {
            options.Cookie.Name = settings.ApplicationName;
            options.SlidingExpiration = true;
            options.ExpireTimeSpan = TimeSpan.FromDays(15);
            options.Cookie.HttpOnly = true;
        });

builder.AddLogRWebMonitor(cfg =>
{
    cfg.HostName = "PalaceServer";
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}
else
{
    app.UseDeveloperExceptionPage();
}

app.UseRouting();
app.MapControllers();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
        .AddInteractiveServerRenderMode();

app.UseLogRWebMonitor();

await app.Services.StartMigration();

var bus = app.Services.GetRequiredService<ArianeBus.IServiceBus>();
await bus.PublishTopic(settings.ServerResetTopicName, new Palace.Shared.Messages.ServerReset());

await app.RunAsync();