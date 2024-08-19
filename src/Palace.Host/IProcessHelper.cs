namespace Palace.Host;

public interface IProcessHelper
{
    List<(int ProcessId, string ServiceName, string? CommandLine)> GetRunningProcess(params string[] mainFileNames);
    Task<List<Palace.Shared.MicroServiceSettings>> GetInstalledServiceList(string installationDirectory);
    Task<(string StartReport, int ProcessId, bool IsStarted)> StartMicroServiceProcess(string hostName, string commandLine, CancellationToken cancellationToken);
    Task WaitForProcessDown(string commandLine);
    Task<(bool Success, string? FailReason)> KillProcess(int processId);
}
