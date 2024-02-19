using System.Reflection;

using Microsoft.AspNetCore.Builder;

using Palace.Server.Services;

namespace Palace.Server;

public static class PluginLoader
{
	private static List<Assembly> _assemblies = new List<Assembly>();

	public static IEnumerable<Assembly> GetPluginAssemblies()
	{
		return _assemblies;
	}

	public static async Task LoadPlugins(WebApplicationBuilder builder)
	{
		var currentFolder = System.IO.Path.GetDirectoryName(typeof(PluginLoader).Assembly.Location)!;
		var pluginAssemblies = System.IO.Directory.GetFiles(currentFolder, $"Palace*.dll");
		foreach (var assemblyFile in pluginAssemblies)
		{
			var assemblyName = assemblyFile.Replace(currentFolder, "").Replace("\\", "").Replace(".dll", "");
			Assembly.Load(assemblyName);
		}
		var assemblies = (AppDomain.CurrentDomain
								.GetAssemblies()
								.Where(a => a.FullName!.StartsWith($"Palace"))).ToList();

		Console.WriteLine($"Found {assemblies.Count} palace assemblies");

		var pluginList = (assemblies
								.SelectMany(i => i.GetTypes().Where(i => !i.IsInterface
									&& typeof(IPalacePlugin).IsAssignableFrom(i)))).ToList();

		Console.WriteLine($"Found {pluginList.Count} palace plugins");

		foreach (var item in pluginList)
		{
			var plugin = (IPalacePlugin)Activator.CreateInstance(item)!;
			await plugin.Configure(builder.Services, builder.Configuration);
			_assemblies.Add(plugin.GetType().Assembly);
		}
	}
}
