using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Palace.Host.Extensions;

public static class GlobalExtensions
{
    public static void Initialize(this Configuration.GlobalSettings palaceSettings)
    {
        var currentDirectory = Path.GetDirectoryName(typeof(Program).Assembly.Location)!;
        if (palaceSettings.UpdateDirectory.StartsWith(@".\"))
        {
            palaceSettings.UpdateDirectory = Path.Combine(currentDirectory, palaceSettings.UpdateDirectory.Replace(@".\", string.Empty));
        }
        if (palaceSettings.DownloadDirectory.StartsWith(@".\"))
        {
            palaceSettings.DownloadDirectory = Path.Combine(currentDirectory, palaceSettings.DownloadDirectory.Replace(@".\", string.Empty));
        }
        if (palaceSettings.InstallationDirectory.StartsWith(@".\"))
        {
            palaceSettings.InstallationDirectory = Path.Combine(currentDirectory, palaceSettings.InstallationDirectory.Replace(@".\", string.Empty));
        }

        if (!Directory.Exists(palaceSettings.UpdateDirectory))
		{
			Directory.CreateDirectory(palaceSettings.UpdateDirectory);
		}
        if (!Directory.Exists(palaceSettings.DownloadDirectory))
        {
            Directory.CreateDirectory(palaceSettings.DownloadDirectory);
        }
        if (!Directory.Exists(palaceSettings.InstallationDirectory))
		{
			Directory.CreateDirectory(palaceSettings.InstallationDirectory);
		}
    }
}
