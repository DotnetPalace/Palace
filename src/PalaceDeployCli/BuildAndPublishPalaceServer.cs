using System.Runtime.CompilerServices;

namespace PalaceDeployCli;

internal class BuildAndPublishPalaceServer(PalaceDeployCliSettings settings)
{
	public async Task PublishServer()
	{
		var build = await Helpers.Process("dotnet", @$"build -c Debug {settings.PalaceServerCsProjectFileName}");
		var publish = await Helpers.Process("dotnet", @$"publish -c Debug {settings.PalaceServerCsProjectFileName}");
		if (!publish.Success)
		{
			Console.WriteLine(publish.Report);
			return;
		}

		var publishPath = Path.GetDirectoryName(settings.PalaceServerCsProjectFileName)!;
		publishPath = System.IO.Path.Combine(publishPath, "bin", "debug", "net8.0", "publish");

		var envTxtFileName = System.IO.Path.Combine(publishPath, $"env.txt");
		await System.IO.File.WriteAllTextAsync(envTxtFileName, settings.Environment);

		var localConfigFileName = System.IO.Path.Combine(publishPath, $"appsettings.local.json");
		System.IO.File.Delete(localConfigFileName);

		await Helpers.Process(@"C:\Program Files\7-Zip\7z.exe", @$"a -tzip -r {publishPath} *", publishPath);

		var zipFileName = System.IO.Path.Combine(publishPath, "..\\", $"publish.zip");
	
	}
}
