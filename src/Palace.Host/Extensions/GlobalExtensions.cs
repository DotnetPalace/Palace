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
        if (palaceSettings.UpdateFolder.StartsWith(@".\"))
        {
            palaceSettings.UpdateFolder = Path.Combine(currentDirectory, palaceSettings.UpdateFolder.Replace(@".\", string.Empty));
        }
        if (palaceSettings.DownloadFolder.StartsWith(@".\"))
        {
            palaceSettings.DownloadFolder = Path.Combine(currentDirectory, palaceSettings.DownloadFolder.Replace(@".\", string.Empty));
        }
        if (palaceSettings.InstallationFolder.StartsWith(@".\"))
        {
            palaceSettings.InstallationFolder = Path.Combine(currentDirectory, palaceSettings.InstallationFolder.Replace(@".\", string.Empty));
        }

        if (!Directory.Exists(palaceSettings.UpdateFolder))
		{
			Directory.CreateDirectory(palaceSettings.UpdateFolder);
		}
        if (!Directory.Exists(palaceSettings.DownloadFolder))
        {
            Directory.CreateDirectory(palaceSettings.DownloadFolder);
        }
        if (!Directory.Exists(palaceSettings.InstallationFolder))
		{
			Directory.CreateDirectory(palaceSettings.InstallationFolder);
		}
    }
}
