﻿using Azure.Storage;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs;

using Microsoft.Extensions.Logging;

using Palace.Shared;
using Palace.PackageDownloader.AzureStorage.Plugin;

namespace Palace.PackageDownloader.AzureStorage;

internal class PackageDownloader : IPackageDownloaderService
{
    private readonly ILogger _logger;
    private readonly IPackageRepository _packageRepository;
    private readonly AzureStorageService _azureStorageService;

    public PackageDownloader(ILogger<PackageDownloader> logger,
        IPackageRepository packageRepository,
        AzureStorageService azureStorageService)
    {
        _logger = logger;
        _packageRepository = packageRepository;
        _azureStorageService = azureStorageService;
    }

    public async Task<string> GenerateUrl(string packageFileName)
    {
        var packageInfo = _packageRepository.GetPackageInfoList().Single(i => i.PackageFileName == packageFileName);

        var keyUri = await _azureStorageService.UploadPackageZipFile(packageInfo);

        return $"{keyUri}";
    }

    public bool IsKeyExists(Guid key)
    {
        return true;
    }

}
