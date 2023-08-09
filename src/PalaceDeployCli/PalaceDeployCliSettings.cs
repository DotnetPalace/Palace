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
		public string LastUpdatePalaceHostUrl { get; set; } = "https://github.com/DotnetPalace/Palace/releases/tag/Latest/palacehost.zip";
		public string LastUpdatePalaceServerUrl { get; set; } = "https://github.com/DotnetPalace/Palace/releases/tag/Latest/palaceserver.zip";
		public string DownloadDirectory { get; set; } = @".\Download";
		public string PalaceHostDeployDirectory { get; set; }
		public string PalaceServerDeployDirectory { get; set; }
		public string PalaceServerWorkerProcessName { get; set; }
	}
}
