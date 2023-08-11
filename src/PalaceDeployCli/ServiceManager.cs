using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace PalaceDeployCli;

public class ServiceManager
{
	public ServiceManager(PalaceDeployCliSettings settings)
	{
		this.Settings = settings;
	}

	protected PalaceDeployCliSettings Settings { get; set; }

	public bool StopService() 
	{
		var isServiceInstalled = IsServiceInstalled(Settings.ServiceName);
		if (!isServiceInstalled)
		{
			Console.WriteLine("The service {0} is not installed", Settings.ServiceName);
			return true;
		}

		var serviceController = new ServiceController(Settings.ServiceName);
		if (serviceController is null)
		{
			// not installed
			return true;
		}

		if (serviceController.Status.Equals(ServiceControllerStatus.Stopped)
			|| serviceController.Status.Equals(ServiceControllerStatus.StopPending))
		{
			Console.WriteLine("Service {0} is already stopped or stopping", Settings.ServiceName);
			return true;
		}

		serviceController.Stop();
		serviceController.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(60));
		return true;
	}

	public bool StartService()
	{
		var isServiceInstalled = IsServiceInstalled(Settings.ServiceName);
		if (!isServiceInstalled)
		{
			return true;
		}

		var serviceController = new ServiceController(Settings.ServiceName);
        if (serviceController is null)
        {
			// not installed
            return true;
        }

		serviceController.Start();
		serviceController.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(60));
		return true;
	}

	private bool IsServiceInstalled(string serviceName)
	{
		try
		{
			var service = new ServiceController(serviceName);
			var status = service.Status; 
			return true;
		}
		catch
		{
			return false;
		}
	}

}
