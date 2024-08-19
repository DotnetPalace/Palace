using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DemoSvc1;

internal static class CommandLineHelper
{
	public static string? GetParameterValue(this string[] args, string parameterName)
	{
		if (args == null
			|| !args.Any())
		{
			return null;
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
			if (item.Equals(parameterName, StringComparison.InvariantCultureIgnoreCase))
			{
				nextisvalue = true;
			}
		}
		return $"{value}".Trim();
	}
}
