using FluentValidation;

using LogRWebMonitor;

using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Palace.Server;
using Palace.Server.Services;
using Palace.WebApp.Services;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Palace.Tests")]

var builder = WebApplication.CreateBuilder(args);

var settings = await builder.AddPalaceServer();

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
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

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
	app.UseExceptionHandler("/Error");
}
else
{
	app.UseDeveloperExceptionPage();
}

app.UseStaticFiles();

app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");
app.MapControllers();

app.UseLogRWebMonitor();

await app.Services.StartMigration();

var bus = app.Services.GetRequiredService<ArianeBus.IServiceBus>();
await bus.PublishTopic(settings.ServerResetTopicName, new Palace.Shared.Messages.ServerReset());

app.Run();