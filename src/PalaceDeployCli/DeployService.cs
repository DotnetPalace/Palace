using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PalaceDeployCli
{
	public class DeployService
	{
		public DeployService(PalaceDeployCliSettings settings,
			ILogger<DeployService> logger)
		{
			this.Settings = settings;
			this.Logger = logger;
		}

		protected PalaceDeployCliSettings Settings { get; }
		protected ILogger Logger { get; }

		public bool UnZipHost(string zipFileName)
		{
			try
			{
				System.IO.Compression.ZipFile.ExtractToDirectory(zipFileName, Settings.PalaceHostDeployDirectory, true);
			}
			catch(Exception ex)
			{
				Logger.LogError(ex, ex.Message);
				return false;
			}
			return true;
		}

		public bool UnZipServer(string zipFileName)
		{
			try
			{
				System.IO.Compression.ZipFile.ExtractToDirectory(zipFileName, Settings.PalaceServerDeployDirectory, true);
			}
			catch (Exception ex)
			{
				Logger.LogError(ex, ex.Message);
				return false;
			}
			return true;
		}

	}
}
