using System.Runtime.CompilerServices;

namespace PalaceDeployCli;

internal class BuildAndPublishPalaceHost(PalaceDeployCliSettings settings)
{
	public async Task<string?> PublisHost()
	{
		var build = await Helpers.Process("dotnet", @$"build -c Debug {settings.PalaceHostCsProjectFileName}");
		var publish = await Helpers.Process("dotnet", @$"publish -c Debug {settings.PalaceHostCsProjectFileName}");
		if (!publish.Success)
		{
			Console.WriteLine(publish.Report);
			return null;
		}

		var publishPath = Path.GetDirectoryName(settings.PalaceHostCsProjectFileName)!;
		publishPath = System.IO.Path.Combine(publishPath, "bin", "debug", "net8.0", "publish");

		var envTxtFileName = System.IO.Path.Combine(publishPath, $"env.txt");
		await System.IO.File.WriteAllTextAsync(envTxtFileName, settings.Environment);

		var localConfigFileName = System.IO.Path.Combine(publishPath, $"appsettings.local.json");
		System.IO.File.Delete(localConfigFileName);

		var zipFileName = System.IO.Path.Combine(publishPath, "..\\", $"publish.zip");
		if (System.IO.File.Exists(zipFileName))
		{
			System.IO.File.Delete(zipFileName);
		}

		await Helpers.Process(@"C:\Program Files\7-Zip\7z.exe", @$"a -tzip -r {publishPath} *", publishPath);

		return zipFileName;
	}
}
