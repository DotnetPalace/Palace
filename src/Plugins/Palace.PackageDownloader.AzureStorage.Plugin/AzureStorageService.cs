using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

using Palace.Shared;

namespace Palace.PackageDownloader.AzureStorage.Plugin;

internal class AzureStorageService
{
    private StorageSharedKeyCredential? _storageSharedKeyCredential = null;
    private readonly AzureStorageConfiguration _settings;
    private readonly ISecretValueReader _secretValueReader;
    private bool _containerExists = false;

    public AzureStorageService(AzureStorageConfiguration settings,
        ISecretValueReader secretValueReader)
    {
        _settings = settings;
        _secretValueReader = secretValueReader;
    }

    public async Task<Uri> UploadPackageZipFile(PackageInfo packageInfo)
    {
        var keyUri = new Uri($"https://{_settings.AccountName}.blob.core.windows.net/{_settings.RepositoryContainer}/{Guid.NewGuid()}/{packageInfo.PackageFileName}");

        var storageCredential = await GetStorageCredential();
        await CreateContainerIfNotExists();
        var blobClient = new BlobClient(keyUri, storageCredential);

        var headers = new BlobHttpHeaders()
        {
            ContentType = "application/zip"
        };
        using var upload = System.IO.File.OpenRead(packageInfo.Location);

        await blobClient.UploadAsync(packageInfo.Location, headers);
        upload.Close();

        return keyUri;
    }

    internal async Task DeleteExpiredPackageFiles(CancellationToken cancellationToken)
    {
        var storageCredential = await GetStorageCredential();
        await CreateContainerIfNotExists();
        var uri = new Uri($"https://{_settings.AccountName}.blob.core.windows.net");
        var blobClient = new BlobServiceClient(uri, storageCredential);
        var container = blobClient.GetBlobContainerClient(_settings.RepositoryContainer);

        var pages = container.GetBlobsAsync(cancellationToken: cancellationToken).AsPages(default, 50);

        await foreach (Azure.Page<BlobItem> page in pages)
        {
            foreach (var blobItem in page.Values)
            {
                var createdDate = blobItem.Properties.CreatedOn?.LocalDateTime;
                if (createdDate.HasValue
                    && createdDate.Value.AddMinutes(15) < DateTime.Now)
                {
                    await container.DeleteBlobIfExistsAsync(blobItem.Name);
                }
            }
        }
    }


    private async Task<StorageSharedKeyCredential> GetStorageCredential()
    {
        if (_storageSharedKeyCredential is not null)
        {
            return _storageSharedKeyCredential;
        }

        var azureStorageAccountKey = _settings.AccountKey;
        if (azureStorageAccountKey is null)
        {
            azureStorageAccountKey = await _secretValueReader.GetSecretValue(_settings.AccountKeySecretName!);
        }
        _storageSharedKeyCredential = new StorageSharedKeyCredential(_settings.AccountName, azureStorageAccountKey);
        return _storageSharedKeyCredential;
    }

    private async Task CreateContainerIfNotExists()
    {
        if (_containerExists)
        {
            return;
        }

        var storageCredential = await GetStorageCredential();
        var blobServiceClient = new BlobServiceClient(new Uri($"https://{_settings.AccountName}.blob.core.windows.net/"), storageCredential);
        var containerClient = blobServiceClient.GetBlobContainerClient(_settings.RepositoryContainer);
        await containerClient.CreateIfNotExistsAsync(publicAccessType: PublicAccessType.Blob);

        _containerExists = true;
    }
}
