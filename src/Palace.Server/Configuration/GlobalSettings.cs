namespace Palace.Server.Configuration;

public class GlobalSettings : Palace.Shared.GlobalSettings
{
    public string ApplicationName { get; set; } = "Palace";
    public string AdminKey { get; private set; } = null!;
    public void SetAdminKey(string key) => AdminKey = key;
     
    public string RepositoryFolder { get; set; } = @".\Repository";
    public string DataFolder { get; set; } = @".\Datas";
    public string StagingFolder { get; set; } = @".\Staging";
    public string BackupFolder { get; set; } = @".\Backup";
    public string TempFolder { get; set; } = @".\Temp";
    public int BackupRetentionCount { get; set; } = 5;

    public string CurrentUrl { get; set; } = null!;
}
