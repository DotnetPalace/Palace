using System;
using System.Collections.Generic;
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
					if (cmdLine.Contains($"{mainFileName}.dll"))
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
}
