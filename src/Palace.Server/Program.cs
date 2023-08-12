
using ArianeBus;

using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;

using Palace.Server.Services;

using FluentValidation;
using LogRWebMonitor;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("Palace.Tests")]

var builder = WebApplication.CreateBuilder(args);

var currentAssembly = typeof(Program).Assembly;
var currentPath = Path.GetDirectoryName(typeof(Program).Assembly.Location)!;

builder.Configuration.SetBasePath(currentPath)
            .AddJsonFile("appSettings.json", false, false)
            .AddJsonFile($"appSettings.{builder.Environment.EnvironmentName}.json", true, false)
            .AddJsonFile("appSettings.local.json", true, false)
            .AddEnvironmentVariables();

var section = builder.Configuration.GetSection("Palace");
var settings = new Palace.Server.Configuration.GlobalSettings();
section.Bind(settings);

builder.Services.AddSingleton(settings);

settings.PrepareFolders(); 

var vaultUri = new Uri($"https://{settings.KeyVaultName}.vault.azure.net");
var credential = new ClientSecretCredential(settings.KeyVaultTenantId, settings.KeyVaultClientId, settings.KeyVaultClientSecret);
var client = new SecretClient(vaultUri, credential);

var apiKeySecret = await client.GetSecretAsync("ApiKey");
settings.SetApiKey(new Guid(apiKeySecret.Value.Value));

var azureBusConnectionStringSecret = await client.GetSecretAsync("AzureBusConnectionString");
settings.SetAzureBusConnectionString(azureBusConnectionStringSecret.Value.Value);

var adminKeySecret = await client.GetSecretAsync("AdminKey");
settings.SetAdminKey(adminKeySecret.Value.Value);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddControllers();

builder.Services.AddSingleton<AdminLoginContext>();
builder.Services.AddScoped<ClipboardService>();
builder.Services.AddDbContextFactory<PalaceDbContext>(lifetime: ServiceLifetime.Transient);
builder.Services.AddSingleton<Orchestrator>();
builder.Services.AddScoped<DialogService>();
builder.Services.AddHostedService<PackageRepositoryWatcher>();
builder.Services.AddHostedService<UpdaterService>();
builder.Services.AddHostedService<CleanerService>();
builder.Services.AddHostedService<HealthCheckerService>();

builder.Services.AddTransient<Palace.Server.Services.UpdateHandler.IUpdateHandler, 
    Palace.Server.Services.UpdateHandler.SaveServiceStateHandler>();
builder.Services.AddTransient<Palace.Server.Services.UpdateHandler.IUpdateHandler, 
    Palace.Server.Services.UpdateHandler.StopServiceHandler>();
builder.Services.AddTransient<Palace.Server.Services.UpdateHandler.IUpdateHandler, 
    Palace.Server.Services.UpdateHandler.InstallServiceHandler>();
builder.Services.AddTransient<Palace.Server.Services.UpdateHandler.IUpdateHandler, 
    Palace.Server.Services.UpdateHandler.StartServiceHandler>();

var sqliteSettings = new Palace.Server.Configuration.SqliteSettings();
sqliteSettings.ConnectionString = $"Data Source={settings.DataFolder}\\Palace.db";
builder.Services.AddSingleton(sqliteSettings);

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

builder.Services.AddArianeBus(config =>
{
    config.BusConnectionString = settings.AzureBusConnectionString;
    config.RegisterQueueReader<Palace.Server.MessageReaders.ServiceInstallationReport>(new QueueName(settings.InstallationReportQueueName));
    config.RegisterQueueReader<Palace.Server.MessageReaders.ServiceHealthCheck>(new QueueName(settings.ServiceHealthQueueName));
    config.RegisterQueueReader<Palace.Server.MessageReaders.ServiceStartingReport>(new QueueName(settings.StartingServiceReportQueueName));
    config.RegisterQueueReader<Palace.Server.MessageReaders.HostHealthCheck>(new QueueName(settings.HostHealthCheckQueueName));
    config.RegisterQueueReader<Palace.Server.MessageReaders.StopServiceReport>(new QueueName(settings.StopServiceReportQueueName));
    config.RegisterQueueReader<Palace.Server.MessageReaders.ServiceUnInstallationReport>(new QueueName(settings.UnInstallationReportQueueName));
    
});

builder.Services.AddValidatorsFromAssembly(currentAssembly);

builder.AddLogRWebMonitor(cfg =>
{
    if (builder.Environment.IsDevelopment())
    {
        cfg.LogLevel = LogLevel.Trace;
    }
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