using System.Diagnostics;

namespace Palace.Host;
public class LinuxProcessHelper(ILogger<LinuxProcessHelper> logger) 
    : ProcessHelperBase(logger)
{
    public override List<(int ProcessId, string ServiceName, string? CommandLine)> GetRunningProcess(params string[] mainFileNames)
    {
        var result = new List<(int ProcessId, string ServiceName, string? CommandLine)>();
        var processes = Process.GetProcessesByName("dotnet");
        foreach (var process in processes)
        {
            var cmd = $"/proc/{process.Id}/cmdline";
            var cmdLine = System.IO.File.ReadAllText(cmd);
            cmdLine = cmdLine.Replace('\0', ' ');
            var mainFileName = mainFileNames.FirstOrDefault(x => cmdLine.Contains(x, StringComparison.InvariantCultureIgnoreCase));
            if (mainFileName is null)
            {
                continue;
            }
            result.Add((process.Id, mainFileName, cmdLine));
        }
        return result;

    }
}
