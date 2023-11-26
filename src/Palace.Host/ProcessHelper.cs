using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;

namespace Palace.Host;

internal static class ProcessHelper
{
	public static List<(int ProcessId, string ServiceName, string? CommandLine)> GetRunningProcess(params string[] mainFileNames)
	{
		var result = new List<(int ProcessId, string ServiceName, string? CommandLine)>();
		var processes = System.Diagnostics.Process.GetProcessesByName("dotnet");
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

	public static async Task<List<Palace.Shared.MicroServiceSettings>> GetInstalledServiceList(string installationDirectory)
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

	public static async Task<(string StartReport, int ProcessId, bool IsStarted)> StartMicroServiceProcess(string commandLine)
	{
		var mre = new System.Threading.ManualResetEvent(false);
		var psi = new ProcessStartInfo("dotnet");

		var report = new StringBuilder();
		int processId = 0;
		bool isStared = false;
		bool hasError = false;

		psi.Arguments = commandLine;

		psi.CreateNoWindow = false;
		psi.UseShellExecute = false;
		psi.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
		psi.RedirectStandardError = true;
		psi.RedirectStandardOutput = false;
		psi.ErrorDialog = false;

		var process = new Process();
		process.StartInfo = psi;
		process.EnableRaisingEvents = true;
		process.ErrorDataReceived += (s, arg) =>
		{
			hasError = true;
			if (string.IsNullOrWhiteSpace(arg.Data))
			{
				return;
			}
			report.AppendLine(arg.Data);
		};

		var start = process.Start();
		if (start)
		{
			processId = process.Id;
			isStared = true;
		}
		else
		{
			report.AppendLine("Process start failed");
			isStared = false;
		}

		process.BeginErrorReadLine();
        await Task.Delay(1 * 1000);

        int loop = 0;
		while (true)
		{
			await Task.Delay(1 * 1000);
			if (!hasError)
			{
				break;
			}

			loop++;
			if (loop > 30)
			{
				break;
			}
		}

		return (report.ToString(), processId, isStared);
	}

	public static async Task WaitForProcessDown(string commandLine)
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

	public static async Task<(bool Success, string? FailReason)> KillProcess(int processId)
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
