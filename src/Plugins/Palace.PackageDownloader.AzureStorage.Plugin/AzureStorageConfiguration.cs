namespace Palace.PackageDownloader.AzureStorage;

public class AzureStorageConfiguration
{
    public string? AccountKey { get; set; }
    public string? AccountKeySecretName { get; set; }
    public string AccountName { get; set; } = "dotnetpalace";
    public string RepositoryContainer { get; set; } = @"download";
}