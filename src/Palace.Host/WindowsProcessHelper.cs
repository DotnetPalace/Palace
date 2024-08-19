using System.Diagnostics;
using System.Management;

namespace Palace.Host;

public class WindowsProcessHelper(ILogger<WindowsProcessHelper> logger) : ProcessHelperBase(logger)
{
    public override List<(int ProcessId, string ServiceName, string? CommandLine)> GetRunningProcess(params string[] mainFileNames)
    {
        var result = new List<(int ProcessId, string ServiceName, string? CommandLine)>();
        var processes = Process.GetProcessesByName("dotnet");
        foreach (var process in processes)
        {
            var searcher = new ManagementObjectSearcher($"SELECT CommandLine FROM Win32_Process WHERE ProcessId = {process.Id}");
            foreach (var obj in searcher.Get())
            {
                var cmdLine = $"{obj["CommandLine"]}";
                foreach (var mainFileName in mainFileNames)
                {
                    if (cmdLine.Contains(mainFileName))
                    {
                        result.Add((process.Id, mainFileName, cmdLine));
                    }
                }
            }
        }
        return result;
    }
}
