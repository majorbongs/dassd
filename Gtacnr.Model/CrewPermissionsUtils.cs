using System;
using System.Collections.Generic;

namespace Gtacnr.Model;

public static class CrewPermissionsUtils
{
	public static bool HasAny(this CrewPermissions value, CrewPermissions any)
	{
		return (value & any) != 0;
	}

	public static bool HasAll(this CrewPermissions value, CrewPermissions all)
	{
		return (value & all) == all;
	}

	public static string PermsToStrings(this CrewPermissions perms)
	{
		List<string> list = new List<string>();
		foreach (CrewPermissions value in Enum.GetValues(typeof(CrewPermissions)))
		{
			if (value != CrewPermissions.None && perms.HasAny(value))
			{
				list.Add(value.ToString());
			}
		}
		if (list.Count == 0)
		{
			return "None";
		}
		return string.Join(", ", list);
	}
}
