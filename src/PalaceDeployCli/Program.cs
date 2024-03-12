
// 1 - Get command line parameters
//      --mode service --servicename xxx (update host palace)
//      --mode webserver --workerprocess xxx (update web server)

// 2 - Download latest version of service or webserver
//

// 3 -- if service
//      stop service and waiting
//      update files
//      start service

// 3bis -- if webserver
//         stop workerprocess and wait
//         update files
//         start workerprocess

using CommandLine;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Web.Administration;
using PalaceDeployCli;
using Spectre.Console;

var configurationBuilder = new ConfigurationBuilder();
configurationBuilder.AddJsonFile("appSettings.json", false);
configurationBuilder.AddJsonFile("appSettings.local.json", true);
var configuration = configurationBuilder.Build();

var settings = new PalaceDeployCliSettings();
var section = configuration.GetSection("PalaceDeployCli");
section.Bind(settings);

if (settings.DownloadDirectory.StartsWith(@".\"))
{
	var currentDirectory = System.IO.Path.GetDirectoryName(typeof(Program).Assembly.Location)!;
	var downloadDirectory = System.IO.Path.Combine(currentDirectory, settings.DownloadDirectory);
	if (!System.IO.Directory.Exists(downloadDirectory))
	{
		System.IO.Directory.CreateDirectory(downloadDirectory);
	}
	settings.DownloadDirectory = downloadDirectory;
}

var services = new ServiceCollection();
services.AddTransient<DownloadManager>();
services.AddTransient<IISManager>();
services.AddTransient<ServiceManager>();
services.AddTransient<DeployService>();
services.AddTransient<BuildAndPublishPalaceServer>();
services.AddTransient<BuildAndPublishPalaceHost>();
services.AddSingleton(settings);
services.AddHttpClient("Downloader", configure =>
{
	configure.DefaultRequestHeaders.UserAgent.ParseAdd($"PalaceUpdater 1.0 ({System.Environment.OSVersion}; {System.Environment.MachineName})");
});

services.AddLogging(setup =>
{
	setup.AddConsole();
});

var sp = services.BuildServiceProvider();

start:

var version = typeof(Program).Assembly.GetName().Version;

AnsiConsole.Markup($"[green]Welcome to the Palace Deploy CLI ({version}) [/]");
AnsiConsole.WriteLine();

var table = new Table();
table.AddColumn("Action").AddColumn("Description");
table.AddRow("1", "Build, Publish Palace.WebApp and install from local");
table.AddRow("2", "Build, Publish Palace.Host and install from local");
table.AddRow("3", "Install latest version of palace host from github");
table.AddRow("4", "Install latest version of palace webapp from github");

table.AddRow("0", "Quit");

AnsiConsole.Write(table);

var selectedAction = AnsiConsole.Prompt(new TextPrompt<int>("Choose your action :"));

var dlManager = sp.GetRequiredService<DownloadManager>();
var deployService = sp.GetRequiredService<DeployService>();
string? zipFileName = null;

switch (selectedAction)
{
	case 1:
		var buildServer = sp.GetRequiredService<BuildAndPublishPalaceServer>();
		zipFileName = await buildServer.PublishServer();
		if (zipFileName is null)
		{
			Console.WriteLine("No zip available");
			break;
		}
		var localIisManager = sp.GetRequiredService<IISManager>();
		localIisManager.StopIISWorkerProcess();
		localIisManager.WaitForStop();
		var localDeployServer = deployService.UnZipServer(zipFileName);
		if (!localDeployServer)
		{
			AnsiConsole.WriteLine("Deploy failed");
			return -1;
		}
		localIisManager.StartIISWorkerProcess();
		break;

	case 2:
		var publishHost = sp.GetRequiredService<BuildAndPublishPalaceHost>();
		zipFileName = await publishHost.PublisHost();
		if (zipFileName is null)
		{
			Console.WriteLine("No zip available");
			break;
		}
		var localServiceManager = sp.GetRequiredService<ServiceManager>();
		await localServiceManager.StopService();
		var localDeployHost = deployService.UnZipHost(zipFileName!);
		if (!localDeployHost)
		{
			AnsiConsole.WriteLine("Deploy failed");
			return -1;
		}
		localServiceManager.StartService();
		break;

	case 3:
		zipFileName = await dlManager.DownloadPackage(settings.LastUpdatePalaceHostUrl);
		if (zipFileName == null)
		{
			AnsiConsole.WriteLine("Download failed");
			return -1;
		}
		var serviceManager = sp.GetRequiredService<ServiceManager>();
		await serviceManager.StopService();
		var deployHost = deployService.UnZipHost(zipFileName);
		if (!deployHost)
		{
			AnsiConsole.WriteLine("Deploy failed");
			return -1;
		}
		serviceManager.StartService();

		break;
	case 4:
		zipFileName = await dlManager.DownloadPackage(settings.LastUpdatePalaceServerUrl);
		if (zipFileName == null)
		{
			AnsiConsole.WriteLine("Download failed");
			return -1;
		}
		var iisManager = sp.GetRequiredService<IISManager>();
		iisManager.StopIISWorkerProcess();
		iisManager.WaitForStop();
		var deployServer = deployService.UnZipServer(zipFileName);
		if (!deployServer)
		{
			AnsiConsole.WriteLine("Deploy failed");
			return -1;
		}
		iisManager.StartIISWorkerProcess();
		break;

	default:
		Environment.Exit(0);
		return 0;
}

goto start;

