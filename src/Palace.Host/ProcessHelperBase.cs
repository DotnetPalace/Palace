using System.Diagnostics;
using System.Text;

namespace Palace.Host;

public abstract class ProcessHelperBase(
    ILogger<ProcessHelperBase> logger
    )
    : IProcessHelper
{
    public abstract List<(int ProcessId, string ServiceName, string? CommandLine)> GetRunningProcess(params string[] mainFileNames);

    public virtual async Task<List<Palace.Shared.MicroServiceSettings>> GetInstalledServiceList(string installationDirectory)
    {
        var fileList = from file in System.IO.Directory.GetFiles(installationDirectory, "servicesettings.json", SearchOption.AllDirectories)
                       select file;

        var result = new List<Palace.Shared.MicroServiceSettings>();

        foreach (var file in fileList)
        {
            var settingsContent = await System.IO.File.ReadAllTextAsync(file);
            var settings = System.Text.Json.JsonSerializer.Deserialize<Shared.MicroServiceSettings>(settingsContent)!;
            result.Add(settings);
        }

        return result;
    }

    public virtual async Task<(string StartReport, int ProcessId, bool IsStarted)> StartMicroServiceProcess(string commandLine, CancellationToken cancellationToken)
    {
        var psi = new ProcessStartInfo("dotnet");

        var report = new StringBuilder();
        int processId = 0;
        bool isStared = false;
        bool hasError = false;

        psi.Arguments = commandLine;

        psi.CreateNoWindow = true;
        psi.UseShellExecute = false;
        psi.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
        psi.RedirectStandardError = true;
        psi.RedirectStandardOutput = false;
        psi.ErrorDialog = false;

        var process = new Process();
        process.StartInfo = psi;
        process.EnableRaisingEvents = true;
        process.ErrorDataReceived += (p, args) =>
        {
            if (args.Data is null)
            {
                return;
            }
            logger.LogDebug(args.Data);
            report.AppendLine(args.Data);
            hasError = true;
        };

        logger.LogInformation("try to start ms with command line : 'dotnet {CommandLine}'", commandLine);
        var start = process.Start();
        if (start)
        {
            processId = process.Id;
            logger.LogInformation("process started with id {ProcessId}", processId);
            isStared = true;
        }
        else
        {
            logger.LogError("process start failed");
            isStared = false;
        }

        process.BeginErrorReadLine();

        int loop = 0;
        while (true)
        {
            if (!hasError)
            {
                break;
            }

            loop++;
            if (loop > 30)
            {
                break;
            }

            await Task.Delay(1 * 1000);
        }

        return (report.ToString(), processId, isStared);
    }

    public virtual async Task WaitForProcessDown(string commandLine)
    {
        var loop = 0;
        while (true)
        {
            var runningProcesses = GetRunningProcess(commandLine);
            if (runningProcesses.Count == 0)
            {
                return;
            }
            loop++;
            if (loop > 30)
            {
                throw new Exception("Wait for process down timeout");
            }
            await Task.Delay(1 * 1000);
        }
    }

    public virtual async Task<(bool Success, string? FailReason)> KillProcess(int processId)
    {
        var process = System.Diagnostics.Process.GetProcessById(processId);
        string? exception = null;
        try
        {
            process.Kill();
        }
        catch (Exception ex)
        {
            exception = ex.Message;
        }
        if (exception is null)
        {
            await WaitForProcessDown(processId.ToString());
        }
        return (exception is null, exception);
    }
}
