using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;

namespace Palace.Tests;

/// <summary>
/// Demarre sans configuration de micro service
/// 
/// Ajoute un service manuellement
/// 
/// Verifie que celui-ci n'étant pas déployé n'est pas démarré
/// 
/// </summary>
[TestClass]
public class UseCase1Test
{
	[TestMethod]
	public async Task Start()
	{
		var host = TestsHelper.CreateTestHostWithServer();
		TestsHelper.CleanupFolders(host);

		await Task.Yield();

		//await msm.AddOrUpdateService(new Models.MicroServiceSettings
		//{
		//	PackageFileName = "DemoSvc.zip",
		//	ServiceName = "DemoSvc",
		//	MainAssembly = "DemoSvc.dll",
		//	Arguments = "--port 12346",
		//	AdminServiceUrl = "http://localhost:12346",
		//	PalaceApiKey = "test"
		//});
	}
}
