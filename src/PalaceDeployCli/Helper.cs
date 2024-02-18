using System.Diagnostics;

namespace PalaceDeployCli;

public static class Helpers
{
	public static async Task<(bool Success, string Report)> Process(string command, string arguments, string? workingDirectory = null)
	{
		await Task.Yield();
		var hasError = false;
		System.Text.StringBuilder sb = new();
		var psi = new System.Diagnostics.ProcessStartInfo(command);
		psi.Arguments = arguments;
		psi.CreateNoWindow = false;
		psi.UseShellExecute = false;
		psi.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
		psi.RedirectStandardError = true;
		psi.ErrorDialog = false;
		if (workingDirectory != null)
		{
			psi.WorkingDirectory = workingDirectory;
		}

		var mre = new System.Threading.ManualResetEvent(false);

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
			sb.AppendLine(arg.Data);
		};
		process.Exited += (s, arg) =>
		{
			Console.WriteLine(arg);
			mre.Set();
		};

		var start = process.Start();
		if (!start)
		{
			throw new Exception();
		}

		/*
        process.BeginErrorReadLine();

        await Task.Delay(4 * 1000);

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
        */

		mre.WaitOne();
		mre.Reset();

		return (!hasError, sb.ToString());
	}

	public static string GetParameterValue(this string[] args, string parameterName)
	{
		if (args == null
			|| !args.Any())
		{
			return string.Empty;
		}

		string value = string.Empty;
		var nextisvalue = false;
		foreach (var item in args)
		{
			if (nextisvalue)
			{
				value = item;
				break;
			}
			if (item.Equals($"{parameterName}", StringComparison.InvariantCultureIgnoreCase))
			{
				nextisvalue = true;
			}
		}
		return $"{value}".Trim();
	}
}