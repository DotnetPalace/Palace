using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace PalaceDeployCli
{
	public class ServiceManager
	{
		public ServiceManager(PalaceDeployCliSettings settings)
		{
			this.Settings = settings;
		}

		protected PalaceDeployCliSettings Settings { get; set; }

		public bool StopService()
		{
			var serviceController = new ServiceController(Settings.ServiceName);
			if (serviceController.Status.Equals(ServiceControllerStatus.Stopped)
				|| serviceController.Status.Equals(ServiceControllerStatus.StopPending))
			{
				Console.WriteLine("Service {0} is already stopped or stopping", Settings.ServiceName);
				return false;
			}

			serviceController.Stop();
			serviceController.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(60));
			return true;
		}

		public bool StartService()
		{
			var serviceController = new ServiceController(Settings.ServiceName);
			if (serviceController.Status.Equals(ServiceControllerStatus.Running))
			{
				Console.WriteLine("Service {0} is already running", Settings.ServiceName);
				return false;
			}

			serviceController.Start();
			serviceController.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(60));
			return true;
		}

	}
}
