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
		public string LastUpdatePalaceHostUrl { get; set; } = "https://github.com/chouteau/Palace/releases/download/Latest/palace.zip";
		public string LastUpdatePalaceServerUrl { get; set; } = "https://github.com/chouteau/Palace/releases/download/Latest/palaceserver.zip";
		public string DownloadDirectory { get; set; } = @".\Download";
		public string PalaceHostDeployDirectory { get; set; }
		public string PalaceServerDeployDirectory { get; set; }
		public string PalaceServerWorkerProcessName { get; set; }

	}
}
