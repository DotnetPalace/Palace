namespace Palace.Server.Services;

public class CleanerService(IPackageRepository packageRepository) : BackgroundService
{
    public override Task StartAsync(CancellationToken cancellationToken)
	{
		return base.StartAsync(cancellationToken);
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		while (!stoppingToken.IsCancellationRequested)
		{
			CleanBackups();
			await Task.Delay(2 * 60 * 1000, stoppingToken);
		}
	}

	private void CleanBackups()
	{
		var packages = packageRepository.GetPackageInfoList();
		foreach (var package in packages)
		{
			var backupList = packageRepository.GetBackupFileList(package.PackageFileName);
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
