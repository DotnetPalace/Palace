namespace Palace.Server.Services;

public class CleanerService : BackgroundService
{
	private readonly Orchestrator _orchestrator;

	public CleanerService(Orchestrator orchestrator)
    {
		_orchestrator = orchestrator;
	}

    public override Task StartAsync(CancellationToken cancellationToken)
	{
		return base.StartAsync(cancellationToken);
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		while (!stoppingToken.IsCancellationRequested)
		{
			CleanBackups();
			await Task.Delay(5 * 60 * 1000, stoppingToken);
		}
	}

	private void CleanBackups()
	{
		var packages = _orchestrator.GetPackageInfoList();
		foreach (var package in packages)
		{
			var backupList = _orchestrator.GetBackupFileList(package.PackageFileName);
			if (backupList.Count > 10)
			{
				var toDelete = backupList.OrderBy(i => i.CreationTime).Take(backupList.Count - 10);
				foreach (var item in toDelete)
				{
					System.IO.Directory.Delete(item.DirectoryName!, true);
				}
			}
		}
	}
}
