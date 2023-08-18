using System.Collections.Concurrent;
using System.Diagnostics;

using ArianeBus;

using Microsoft.Extensions.Caching.Memory;

using Palace.Server.Models;

namespace Palace.Server.Services;

public class Orchestrator
{
    private readonly ConcurrentDictionary<string, Models.ExtendedMicroServiceInfo> _extendedMicroServiceInfoList = new(comparer: StringComparer.InvariantCultureIgnoreCase);
    private readonly ConcurrentDictionary<string, Models.HostInfo> _hostInfoList = new(comparer: StringComparer.InvariantCultureIgnoreCase);

    private readonly ILogger<Orchestrator> _logger;
    private readonly Configuration.GlobalSettings _settings;
	private readonly IServiceBus _bus;
	private readonly LongActionService _longActionService;

    public event Action<Models.HostInfo> HostChanged = default!;
    public event Action<Models.ExtendedMicroServiceInfo> ServiceChanged = default!;
    public event Action<Models.ActionResult> LongActionProgress = default!;

    public Orchestrator(ILogger<Orchestrator> logger,
        Configuration.GlobalSettings settings,
        ArianeBus.IServiceBus bus,
        LongActionService longActionService)
    {
        _logger = logger;
        _settings = settings;
		_bus = bus;
		_longActionService = longActionService;
    }

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

		HostChanged?.Invoke(hostInfo);
    }

    public Models.ExtendedMicroServiceInfo? GetExtendedMicroServiceInfoByKey(string key)
    {
        _extendedMicroServiceInfoList.TryGetValue(key, out var result);
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

        ServiceChanged?.Invoke(emsi);
    }

    public void GlobalReset()
    {
        _extendedMicroServiceInfoList.Clear();
        _hostInfoList.Clear();
        // _packageList.Clear();
        // LoadPackageList();
		_bus.PublishTopic(_settings.ServerResetTopicName, new Palace.Shared.Messages.ServerReset());
        HostChanged?.Invoke(new HostInfo());
	}

    public void OnLongActionProgress(Models.ActionResult actionResult)
    {
        if (LongActionProgress is not null)
		{
			LongActionProgress(actionResult);
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
			ServiceChanged?.Invoke(existing);
		}
	}
}
