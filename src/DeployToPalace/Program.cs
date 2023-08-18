using Microsoft.Extensions.Configuration;

using DeployToPalace;
using System.IO;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

Console.WriteLine("Prepare to palace");
Console.WriteLine("-----------------");

var configurationBuilder = new ConfigurationBuilder()
	.AddJsonFile("appsettings.json")
	.AddJsonFile("appsettings.local.json", true);

var configuration = configurationBuilder.Build();

var deployConfiguration = new DeployConfiguration();
configuration.GetSection("Palace").Bind(deployConfiguration);

var keyvaultName = configuration.GetValue<string>("Palace.KeyVaultProvider:KeyVaultName")!;
var keyVaultTenantId = configuration.GetValue<string>("Palace.KeyVaultProvider:KeyVaultTenantId")!;
var keyVaultClientId = configuration.GetValue<string>("Palace.KeyVaultProvider:KeyVaultClientId")!;
var keyVaultClientSecret = configuration.GetValue<string>("Palace.KeyVaultProvider:KeyVaultClientSecret")!;

var vaultUri = new Uri($"https://{keyvaultName}.vault.azure.net");
var credential = new ClientSecretCredential(keyVaultTenantId, keyVaultClientId, keyVaultClientSecret);
var client = new SecretClient(vaultUri, credential);

var apiKeySecret = client.GetSecretAsync("ApiKey").Result;
var apiKey = apiKeySecret.Value.Value;

var msToDeploy = args.GetParameterValue("--ms");
var environmentName = args.GetParameterValue("--en");
deployConfiguration.EnvironmentName = environmentName ?? deployConfiguration.EnvironmentName ?? "development";

var currentFolder = System.IO.Path.GetDirectoryName(typeof(Program).Assembly.Location)!;
var srcFolder = System.IO.Path.Combine(currentFolder, "..\\..\\..\\..");
srcFolder = new DirectoryInfo(srcFolder).FullName;

var stagingFolder = configuration.GetValue<string>("Palace:DeployStagingFolder")!;
if (stagingFolder.StartsWith(".\\"))
{
	deployConfiguration.StagingFolder = stagingFolder;
}

if (deployConfiguration.StagingFolder.StartsWith(".\\"))
{
	deployConfiguration.StagingFolder = deployConfiguration.StagingFolder.Replace(".\\", "");
	deployConfiguration.StagingFolder = System.IO.Path.Combine(srcFolder, deployConfiguration.StagingFolder);
}

Console.WriteLine(msToDeploy);

foreach (var ms in deployConfiguration.CSProjFileNameList)
{
	var projectName = ms.Split('\\').Last().Replace(".csproj", "",StringComparison.InvariantCultureIgnoreCase);

	if (!string.IsNullOrWhiteSpace(msToDeploy)
		&& projectName != msToDeploy)
	{
		continue;
	}

	var csProjFileName = System.IO.Path.Combine(srcFolder, projectName, $"{projectName}.csproj");
	var csProjContent = System.IO.File.ReadAllText(csProjFileName);
	var versionMatch = System.Text.RegularExpressions.Regex.Match(csProjContent, @"\<Version\>(?<v>[^\<]*)");
	if (versionMatch.Success)
	{
		var versionString = versionMatch.Groups["v"].Value;
		Version.TryParse(versionString, out var version);

		var newVersion = new Version(Math.Max(0, version!.Major),
									Math.Max(0, version.Minor),
									Math.Max(0, version.Build + 1),
									Math.Max(0, version.Revision));
		var left = csProjContent.Substring(0, versionMatch.Index) + "<Version>";
		var right = csProjContent.Substring(versionMatch.Index + versionMatch.Length);
		var newContent = left + $"{newVersion}" + right;
		System.IO.File.WriteAllText(csProjFileName, newContent);
	}

	var publishPath = Path.Combine(srcFolder, projectName, "bin\\debug\\net7.0\\publish");
	if (System.IO.Directory.Exists(publishPath))
	{
		System.IO.Directory.Delete(publishPath, true);
	}

	Helpers.Process("dotnet", @$"publish {csProjFileName}");

	var envTxtFileName = System.IO.Path.Combine(publishPath, $"env.txt");
	System.IO.File.WriteAllText(envTxtFileName, deployConfiguration.EnvironmentName);

	Helpers.Process(@"""C:\Program Files\7-Zip\7z.exe""", @$"a -tzip -r {publishPath} *", publishPath);

	var zipFileName = System.IO.Path.Combine(publishPath, "..\\", $"publish.zip");
	var fileInfo = new System.IO.FileInfo(zipFileName);

	if (deployConfiguration.StagingUrl is not null)
	{
		await Helpers.UploadToServer(fileInfo, projectName, deployConfiguration.StagingUrl, apiKey);
	}
	else if (deployConfiguration.StagingFolder is not null)
	{
		var destination = Path.Combine(deployConfiguration.StagingFolder, $"{projectName}.zip");
		System.IO.File.Copy(zipFileName, destination, true);
	}
}

Console.WriteLine("Process Finished, <hit key to exit>");