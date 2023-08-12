
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
	var currentDirectory = System.IO.Path.GetDirectoryName(typeof(Program).Assembly.Location);
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
table.AddRow("1", "Install latest version of palace host");
table.AddRow("2", "Install latest version of palace server");
table.AddRow("3", "Quit");

AnsiConsole.Write(table);

var selectedAction = AnsiConsole.Prompt(new TextPrompt<int>("Choose your action :"));

if (selectedAction == 3)
{
	return 0;
}

var dlManager = sp.GetRequiredService<DownloadManager>();
var deployService = sp.GetRequiredService<DeployService>();
string zipFileName = null;

switch (selectedAction)
{
	case 1:
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
		var start = serviceManager.StartService();

		break;
	case 2:
		zipFileName = await dlManager.DownloadPackage(settings.LastUpdatePalaceServerUrl);
		if (zipFileName == null)
		{
			AnsiConsole.WriteLine("Download failed");
			return -1;
		}
		var iisManager = sp.GetRequiredService<IISManager>();
		var stopWorker = iisManager.StopIISWorkerProcess();
		iisManager.WaitForStop();
		var deployServer = deployService.UnZipServer(zipFileName);
		if (!deployServer)
		{
			AnsiConsole.WriteLine("Deploy failed");
			return -1;
		}
		var startWorker = iisManager.StartIISWorkerProcess();
		break;
}

goto start;


