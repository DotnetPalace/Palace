using System.Reflection;

using ArianeBus;

using Microsoft.Extensions.Logging;

using Palace.Shared.Results;

namespace Palace.Host.MessageReaders;

public class InstallService : ArianeBus.MessageReaderBase<Shared.Messages.InstallService>
{
    private readonly ILogger<InstallService> _logger;
    private readonly Configuration.GlobalSettings _settings;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IServiceBus _bus;

    public InstallService(ILogger<InstallService> logger,
        Configuration.GlobalSettings settings,
        IHttpClientFactory httpClientFactory,
        ArianeBus.IServiceBus bus)
    {
        _logger = logger;
        _settings = settings;
        _httpClientFactory = httpClientFactory;
        _bus = bus;
    }

    public override async Task ProcessMessageAsync(Shared.Messages.InstallService message, CancellationToken cancellationToken)
    {
        if (message is null)
        {
            _logger.LogError("message is null");
            return;
        }

        if (message.Timeout < DateTime.Now)
        {
            _logger.LogTrace("message is too old");
            return;
        }

        if (!message.HostName.Equals(_settings.HostName))
        {
            _logger.LogTrace("installation service not for me");
            return;
        }

        var report = new Shared.Messages.ServiceInstallationReport
        {
            HostName = _settings.HostName,
            ServiceName = message.ServiceSettings.ServiceName,
            Trigger = message.Trigger
        };

		// On recupere le zip sur le serveur
		var downloadResult = await DownloadPackage(message.DownloadUrl);
        if (!downloadResult.Success)
        {
            _logger.LogWarning("Download zipfile for service {name} failed", message.ServiceSettings.MainAssembly);
            report.Success = false;
            report.FailReason = downloadResult.FailReason;
        }
        else
        {
            report.Success = true;
        }

        try
        {
            await ProcessHelper.WaitForProcessDown(message.ServiceSettings.ServiceName);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Wait for process down failed");
			report.Success = false;
			report.FailReason = ex.Message;
        }

        if (report.Success)
        {
            var installResult = InstallLocalService(message, downloadResult);
            if (!installResult.success)
            {
                _logger.LogWarning("Install service {name} failed", message.ServiceSettings.MainAssembly);
				report.Success = false;
				report.FailReason = installResult.failReason;
			}
		}

        if (report.Success)
        {
            var deployResult = await DeployService(message.ServiceSettings, downloadResult.ZipFileName);
            if (!deployResult.success)
            {
                report.Success = false;
                report.FailReason = deployResult.failReason;
            }
            else
            {
                report.InstallationFolder = deployResult.installationFolder;
            }
        }

		await _bus.EnqueueMessage(_settings.InstallationReportQueueName, report);
	}

	(bool success, string? failReason) InstallLocalService(Shared.Messages.InstallService message, DownloadFileResult zipFileInfo)
    {
		var installationFolder = System.IO.Path.Combine(_settings.InstallationFolder, message.ServiceSettings.ServiceName);

		_logger.LogInformation("Try to install MicroService {MainAssembly} in {installationFolder}", message.ServiceSettings.MainAssembly, installationFolder);

		string? extractDirectory = null;
        // Dezip dans son répertoire avec la bonne version
        extractDirectory = System.IO.Path.Combine(_settings.DownloadFolder, message.ServiceSettings.ServiceName);
        if (Directory.Exists(extractDirectory))
        {
            Directory.Delete(extractDirectory, true);
        }
        _logger.LogInformation("Extact zipfile {0} for service {1} in directory {2}", zipFileInfo.ZipFileName, message.ServiceSettings.ServiceName, extractDirectory);
        if (!System.IO.Directory.Exists(extractDirectory))
        {
            try
            {
                System.IO.Directory.CreateDirectory(extractDirectory);
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, ex.Message);
                return (false, ex.Message);
            }
        }
        System.IO.Compression.ZipFile.ExtractToDirectory(zipFileInfo.ZipFileName, extractDirectory, true);

        return (true, null);
    }

    async Task<DownloadFileResult> DownloadPackage(string downloadUrl)
    {
        var httpClient = _httpClientFactory.CreateClient("PalaceServer");
        var result = new DownloadFileResult();

        HttpResponseMessage? response = null;
        try
        {
            response = await httpClient.GetAsync(downloadUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            result.Success = false;
            result.FailReason = ex.Message;
            return result;
        }

        if (response.StatusCode != System.Net.HttpStatusCode.OK)
        {
            _logger.LogWarning("response fail for download {0}", downloadUrl);
            result.Success = false;
            result.FailReason = $"response fail for download {downloadUrl} with status code {response.StatusCode}";
            return result;
        }

        if (!response.Content.Headers.Contains("content-disposition"))
        {
            _logger.LogWarning("response fail for {0} header content-disposition not found", downloadUrl);
            result.Success = false;
            result.FailReason = $"response fail for {downloadUrl} header content-disposition not found";
			return result;
        }

        var contentDisposition = response.Content.Headers.GetValues("content-disposition").FirstOrDefault();
        if (string.IsNullOrWhiteSpace(contentDisposition))
        {
            _logger.LogWarning("response fail for {0} header content-disposition empty", downloadUrl);
            result.Success = false;
            result.FailReason = $"response fail for {downloadUrl} header content-disposition empty";
			return result;
        }

        if (!System.IO.Directory.Exists(_settings.DownloadFolder))
        {
            System.IO.Directory.CreateDirectory(_settings.DownloadFolder);
        }

        result.ZipFileName = System.IO.Path.Combine(_settings.DownloadFolder, contentDisposition.Split(';')[1].Split('=')[1]);

        if (File.Exists(result.ZipFileName))
        {
            File.Delete(result.ZipFileName);
        }

        using (var fs = new System.IO.FileStream(result.ZipFileName, System.IO.FileMode.Create))
        {
            var stream = response.Content.ReadAsStreamAsync().Result;
            int bufferSize = 1024;
            byte[] buffer = new byte[bufferSize];
            int pos = 0;
            while ((pos = stream.Read(buffer, 0, bufferSize)) > 0)
            {
                fs.Write(buffer, 0, pos);
            }
            fs.Close();
        }

        result.Success = true;
		return result;
	}

	async Task<(bool success, string installationFolder, string? failReason)> DeployService(Shared.MicroServiceSettings serviceSettings, string unZipFileName)
	{
		var deploySuccess = true;
        var unZipFolder = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(unZipFileName)!, serviceSettings.ServiceName);
		var fileList = System.IO.Directory.GetFiles(unZipFolder, "*.*", System.IO.SearchOption.AllDirectories);

        var installationFolder = System.IO.Path.Combine(_settings.InstallationFolder, serviceSettings.ServiceName);

		_logger.LogInformation($"try to deploy {fileList.Count()} files from {unZipFolder} to {installationFolder}");

		try
		{
			if (System.IO.Directory.Exists(installationFolder))
			{
				// Nettoyage global du repertoire de destination
				System.IO.Directory.Delete(installationFolder, true);
				System.IO.Directory.CreateDirectory(installationFolder);
			}
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, ex.Message);
			return (false, installationFolder,  ex.Message);
		}

		foreach (var sourceFile in fileList)
		{
			var destFile = sourceFile.Replace(unZipFolder, "").Trim('\\');
			destFile = System.IO.Path.Combine(installationFolder, destFile);

			var destDirectory = System.IO.Path.GetDirectoryName(destFile)!;
			if (!System.IO.Directory.Exists(destDirectory))
			{
				System.IO.Directory.CreateDirectory(destDirectory);
			}

			var isCopySuccess = await CopyUpdateFile(sourceFile, destFile);
			if (!isCopySuccess)
			{
				deploySuccess = false;
				break;
			}

			if (System.IO.Path.GetFileName(destFile).Equals(serviceSettings.MainAssembly, StringComparison.InvariantCultureIgnoreCase))
			{
				var lastWriteTime = DateTime.Now.AddSeconds(_settings.ScanIntervalInSeconds + 1);
				File.SetLastWriteTime(destFile, lastWriteTime);
			}
		}

		if (!deploySuccess)
		{
			_logger.LogInformation("deploy failed for service {0}", serviceSettings.ServiceName);
            return (false, installationFolder, "deploy failed when try to copy");
		}
        else
        {
            var serviceSettingsContent = System.Text.Json.JsonSerializer.Serialize(serviceSettings);
            var serviceSettingsFile = System.IO.Path.Combine(installationFolder, "servicesettings.json");
            System.IO.File.WriteAllText(serviceSettingsFile, serviceSettingsContent);
		}

		return (true, installationFolder, null);
	}

	private async Task<bool> CopyUpdateFile(string sourceFile, string destFile)
	{
		bool copySuccess = true;
		var loop = 0;
		while (true)
		{
			try
			{
				if (!System.IO.File.Exists(destFile))
				{
					System.IO.File.Delete(destFile);
				}
				System.IO.File.Copy(sourceFile, destFile, true);
				_logger.LogDebug($"Copy {sourceFile} to {destFile}");
				copySuccess = true;
				break;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, ex.Message);
				loop++;
				copySuccess = false;
			}

			if (loop > 3)
			{
				break;
			}

			// Le service n'est peut etre pas encore arreté
			await Task.Delay(2 * 1000);
		}

		return copySuccess;
	}
}
