using System.Diagnostics;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Palace.Tests;

internal static class TestsHelper
{
    public static IHost CreateTestHostWithServer(Action<Palace.Server.Configuration.GlobalSettings>? config = null)
    {
        var configuration = new ConfigurationBuilder()
                                .AddJsonFile("appSettings.json")
                                .Build();

        var palaceSection = configuration.GetSection("Palace");
        var palaceSettings = new Palace.Server.Configuration.GlobalSettings();
        palaceSection.Bind(palaceSettings);

        config?.Invoke(palaceSettings);

        var builder = WebApplication.CreateBuilder();
        var app = builder.Build();

        return app;
    }

    public static void CleanupFolders(IHost host)
    {
        var palaceSettings = host.Services.GetRequiredService<Palace.Server.Configuration.GlobalSettings>();

        var currentDirectory = System.IO.Path.GetDirectoryName(typeof(TestsHelper).Assembly.Location)!;

        var removeDirectoryList = new List<string>
        {
            Path.Combine(currentDirectory, palaceSettings.BackupFolder),
            Path.Combine(currentDirectory, palaceSettings.DataFolder),
            Path.Combine(currentDirectory, palaceSettings.RepositoryFolder),
            Path.Combine(currentDirectory, palaceSettings.StagingFolder),
            Path.Combine(currentDirectory, palaceSettings.TempFolder)
        };

		foreach (var directory in removeDirectoryList)
        {
            if (!System.IO.Directory.Exists(directory))
            {
                continue;
            }
            System.IO.Directory.Delete(directory, true);
        }
    }

    public static void UpdateVersionDemoProject(IHost host)
    {
        var config = host.Services.GetRequiredService<IConfiguration>();
        var demoProject = config["Test:DemoProject"]!;

        var currentDirectory = System.IO.Path.GetDirectoryName(typeof(TestsHelper).Assembly.Location)!;
        demoProject = System.IO.Path.Combine(currentDirectory, demoProject);

        var content = System.IO.File.ReadAllText(demoProject);
        var versionMatch = System.Text.RegularExpressions.Regex.Match(content, @"\<Version\>(?<v>[^\<]*)");
        if (versionMatch.Success)
        {
            var versionString  = versionMatch.Groups["v"].Value;
            Version.TryParse(versionString, out var version);

            var newVersion = new Version(Math.Max(0, version!.Major),
                                        Math.Max(0, version.Minor),
                                        Math.Max(0, version.Build + 1),
                                        Math.Max(0, version.Revision));
            var left = content.Substring(0, versionMatch.Index) + "<Version>";
            var right = content.Substring(versionMatch.Index + versionMatch.Length);
            var newContent = left + $"{newVersion}" + right;
            System.IO.File.Copy(demoProject, $"{demoProject}.bak", true);
            System.IO.File.WriteAllText(demoProject, newContent);
        }
   }

    public static void PublishDemoProject(IHost host, string zipFileName = "demosvc.zip")
    {
        var config = host.Services.GetRequiredService<IConfiguration>();
        var demoProject = config["Test:DemoProject"]!;
        var workingDirectory = config["Test:WorkingDirectory"]!;
        var deployDirectory = config["PalaceServer:MicroServiceRepositoryFolder"]!;

        var currentDirectory = System.IO.Path.GetDirectoryName(typeof(TestsHelper).Assembly.Location)!;
        demoProject = System.IO.Path.Combine(currentDirectory, demoProject);
        workingDirectory = System.IO.Path.Combine(currentDirectory, workingDirectory);
        deployDirectory = System.IO.Path.Combine(currentDirectory, deployDirectory);

        var psi = new System.Diagnostics.ProcessStartInfo("dotnet");

        psi.Arguments = $"publish {demoProject}";
        psi.CreateNoWindow = false;
        psi.UseShellExecute = false;
        psi.WindowStyle = System.Diagnostics.ProcessWindowStyle.Normal;
        psi.RedirectStandardOutput = true;

        var process = new Process();
        process.StartInfo = psi;
        process.Start();

        var reader = process.StandardOutput;
        var output = reader.ReadToEnd();
        reader.Close();

        Console.WriteLine(output);

        var psiZip = new System.Diagnostics.ProcessStartInfo(@"C:\Program Files\7-Zip\7z.exe");
        psiZip.WorkingDirectory = workingDirectory;
        psiZip.Arguments = @$"a -tzip -r {deployDirectory}\{zipFileName} * ";

        psiZip.CreateNoWindow = false;
        psiZip.UseShellExecute = false;
        psiZip.WindowStyle = System.Diagnostics.ProcessWindowStyle.Normal;
        psiZip.RedirectStandardOutput = true;

        process = new Process();
        process.StartInfo = psiZip;
        process.Start();

        reader = process.StandardOutput;
        output = reader.ReadToEnd();
        reader.Close();

        Console.WriteLine(output);
    }
}
