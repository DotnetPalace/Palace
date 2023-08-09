using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Palace.Server.Extensions;

public static class ConfigurationExtensions
{
    public static void PrepareFolders(this Configuration.GlobalSettings settings)
    {
        var directoryName = System.IO.Path.GetDirectoryName(typeof(Program).Assembly.Location)!;
        if (settings.RepositoryFolder.StartsWith(@".\"))
        {
            settings.RepositoryFolder = System.IO.Path.Combine(directoryName, settings.RepositoryFolder.Replace(@".\", ""));
        }
        if (settings.StagingFolder.StartsWith(@".\"))
        {
            settings.StagingFolder = System.IO.Path.Combine(directoryName, settings.StagingFolder.Replace(@".\", ""));
        }
        if (settings.BackupFolder.StartsWith(@".\"))
        {
            settings.BackupFolder = System.IO.Path.Combine(directoryName, settings.BackupFolder.Replace(@".\", ""));
        }
        if (settings.TempFolder.StartsWith(@".\"))
        {
            settings.TempFolder = System.IO.Path.Combine(directoryName, settings.TempFolder.Replace(@".\", ""));
        }

        if (!System.IO.Directory.Exists(settings.RepositoryFolder))
        {
            System.IO.Directory.CreateDirectory(settings.RepositoryFolder);
        }
        if (!System.IO.Directory.Exists(settings.StagingFolder))
        {
            System.IO.Directory.CreateDirectory(settings.StagingFolder);
        }
        if (!System.IO.Directory.Exists(settings.BackupFolder))
        {
            System.IO.Directory.CreateDirectory(settings.BackupFolder);
        }
        if (!System.IO.Directory.Exists(settings.TempFolder))
        {
            System.IO.Directory.CreateDirectory(settings.TempFolder);
        }
    }
}
