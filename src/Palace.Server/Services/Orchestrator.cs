using Palace.Server.Models;

namespace Palace.Server.Services;

public class Orchestrator(
	ILogger<Orchestrator> logger,
	Configuration.GlobalSettings settings,
	ArianeBus.IServiceBus bus
	)
{
	private readonly ConcurrentDictionary<string, Models.ExtendedMicroServiceInfo> 
		_extendedMicroServiceInfoList = new(comparer: StringComparer.InvariantCultureIgnoreCase);
	private readonly ConcurrentDictionary<string, Models.HostInfo> 
		_hostInfoList = new(comparer: StringComparer.InvariantCultureIgnoreCase);

	public event Action<Models.HostInfo> HostChanged = default!;
	public event Action<Models.ExtendedMicroServiceInfo> ServiceChanged = default!;
	public event Action<Models.ActionResult> LongActionProgress = default!;

	public IEnumerable<Models.ExtendedMicroServiceInfo> GetServiceList()
	{
		return _extendedMicroServiceInfoList.Select(i => i.Value);
	}

	public IEnumerable<Models.HostInfo> GetHostList()
	{
		return _hostInfoList.Select(i => i.Value);
	}

	public void AddOrUpdateHost(Models.HostInfo hostInfo)
	{
		_hostInfoList.TryGetValue(hostInfo.HostName, out var existing);
		if (existing is null)
		{
			existing = hostInfo;
			_hostInfoList.TryAdd(hostInfo.HostName, hostInfo);
		}

		existing.LastHitDate = DateTime.Now;
		existing.ExternalIp = hostInfo.ExternalIp;
		existing.Version = hostInfo.Version;
		existing.HostState = hostInfo.HostState;
		existing.MainFileName = hostInfo.MainFileName;
		existing.TotalDriveSize = hostInfo.TotalDriveSize;
		existing.TotalFreeSpaceOfDriveSize = hostInfo.TotalFreeSpaceOfDriveSize;
		existing.OsDescription = hostInfo.OsDescription;
		existing.OsVersion = hostInfo.OsVersion;
		existing.ProcessId = hostInfo.ProcessId;
		existing.PercentCpu = hostInfo.PercentCpu;

		try
		{
			HostChanged?.Invoke(hostInfo);
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Error while adding or updating host");
		}
	}

	public Models.ExtendedMicroServiceInfo? GetExtendedMicroServiceInfoByKey(string key)
	{
		var success = _extendedMicroServiceInfoList.TryGetValue(key, out var result);
		if (!success)
		{
			logger.LogWarning("GetExtendedMicroServiceInfoByKey - Could not find key {key}", key);
			return null;
		}
		return result;
	}

	public void AddOrUpdateMicroServiceInfo(Models.ExtendedMicroServiceInfo microserviceInfo)
	{
		_extendedMicroServiceInfoList.TryGetValue(microserviceInfo.Key, out var emsi);
		if (emsi is null)
		{
			emsi = new Models.ExtendedMicroServiceInfo();
			emsi.HostName = microserviceInfo.HostName;
			emsi.ServiceName = microserviceInfo.ServiceName;
			_extendedMicroServiceInfoList.TryAdd(emsi.Key, emsi);
		}

		emsi!.Location = microserviceInfo.Location;
		emsi.UserInteractive = microserviceInfo.UserInteractive;
		if (string.IsNullOrWhiteSpace(microserviceInfo.Version))
		{
			emsi.Version = microserviceInfo.Version;
		}
		emsi.LastWriteTime = microserviceInfo.LastWriteTime;
		if (microserviceInfo.ThreadCount > 0)
		{
			emsi.ThreadCount = microserviceInfo.ThreadCount;
			emsi.ThreadCountHistory.Add(new PerformanceCounter
			{
				Value = microserviceInfo.ThreadCount,
			});
			if (emsi.ThreadCountHistory.Count > 100)
			{
				emsi.ThreadCountHistory.RemoveAt(0);
			}
		}
		emsi.ProcessId = microserviceInfo.ProcessId;
		emsi.ServiceState = microserviceInfo.ServiceState;
		emsi.StartedDate = microserviceInfo.StartedDate;
		emsi.CommandLine = microserviceInfo.CommandLine;
		emsi.PeakPagedMem = microserviceInfo.PeakPagedMem;
		emsi.PeakVirtualMem = microserviceInfo.PeakVirtualMem;
		emsi.PeakWorkingSet = microserviceInfo.PeakWorkingSet;
		if (microserviceInfo.WorkingSet > 0)
		{
			emsi.WorkingSet = microserviceInfo.WorkingSet;
			emsi.WorkingSetHistory.Add(new PerformanceCounter
			{
				Value = microserviceInfo.WorkingSet,
			});
			if (emsi.WorkingSetHistory.Count > 100)
			{
				emsi.WorkingSetHistory.RemoveAt(0);
			}
		}
		emsi.CommandLine = microserviceInfo.CommandLine;
		emsi.EnvironmentName = microserviceInfo.EnvironmentName;
		emsi.Version = microserviceInfo.Version;
		emsi.LastHitDate = microserviceInfo.LastHitDate;
		emsi.Log = microserviceInfo.Log;
		emsi.FailReason = microserviceInfo.FailReason;

		try
		{
			ServiceChanged?.Invoke(emsi);
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Error while adding or updating microservice");
		}

	}

	public void GlobalReset()
	{
		_extendedMicroServiceInfoList.Clear();
		_hostInfoList.Clear();
		bus.PublishTopic(settings.ServerResetTopicName, new Palace.Shared.Messages.ServerReset());
		try
		{
			HostChanged?.Invoke(new HostInfo());
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Error while global reset");
		}
	}

	public void OnLongActionProgress(Models.ActionResult actionResult)
	{
		if (LongActionProgress is not null)
		{
			try
			{
				LongActionProgress(actionResult);
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Error while long action progress");
			}
		}
	}

	public void OnServicesChanged()
	{
		if (ServiceChanged is not null)
		{
			try
			{
				ServiceChanged.Invoke(new());
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Error while services changed");
			}
		}
	}

	internal void RemoveMicroServiceInfo(ExtendedMicroServiceInfo rmi)
	{
		if (rmi is null)
		{
			return;
		}

		_extendedMicroServiceInfoList.Remove(rmi.Key, out var existing);
		if (existing is not null)
		{
			try
			{
				ServiceChanged?.Invoke(existing);
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Error while removing microservice info");
			}
		}
	}
}
