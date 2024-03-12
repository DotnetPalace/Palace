using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PalaceDeployCli
{
	public class PalaceDeployCliSettings
	{
		public string ServiceName { get; set; } = "Palace";
		public string LastUpdatePalaceHostUrl { get; set; } = "https://github.com/DotnetPalace/Palace/releases/download/Latest/palacehost.zip";
		public string LastUpdatePalaceServerUrl { get; set; } = "https://github.com/DotnetPalace/Palace/releases/download/Latest/palacewebapp.zip";
		public string DownloadDirectory { get; set; } = @".\Download";
		public string PalaceHostDeployDirectory { get; set; } = null!;
		public string PalaceServerDeployDirectory { get; set; } = null!;
		public string PalaceServerCsProjectFileName { get; set; } = null!;
		public string PalaceServerWorkerProcessName { get; set; } = null!;
		public string PalaceHostCsProjectFileName { get; set; } = null!;
		public string Environment { get; set; } = "development";
    }
}
