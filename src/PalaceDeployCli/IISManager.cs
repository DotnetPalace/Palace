using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Web.Administration;

namespace PalaceDeployCli
{
	public class IISManager
	{
		public IISManager(PalaceDeployCliSettings settings)
		{
			this.Settings = settings;
		}			

		protected PalaceDeployCliSettings Settings { get; }

		public string StopIISWorkerProcess()
		{
			using var serverManager = new ServerManager();
			var w3wp = serverManager.ApplicationPools[Settings.PalaceServerWorkerProcessName];
			var result = w3wp.Stop();
			return $"{result}";
		}

		public void WaitForStop()
		{
			var loop = 0;
			while(true)
			{
				using var serverManager = new ServerManager();
				var w3wp = serverManager.ApplicationPools[Settings.PalaceServerWorkerProcessName];
				var state = $"{w3wp.State}";
				if (state.Equals("Stopped", StringComparison.InvariantCultureIgnoreCase))
				{
					break;
				}
				loop++;
				if (loop > 120)
				{
					throw new Exception("Worker process not stopped");
				}
				System.Threading.Thread.Sleep(1000);
			}
		}

		public string StartIISWorkerProcess()
		{
			using var serverManager = new ServerManager();
			var w3wp = serverManager.ApplicationPools[Settings.PalaceServerWorkerProcessName];
			var result = w3wp.Start();
			return $"{result}";
		}

	}
}
