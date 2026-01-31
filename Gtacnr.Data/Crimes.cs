using System.Collections.Generic;
using Gtacnr.Model;
using Gtacnr.Model.Enums;

namespace Gtacnr.Data;

public static class Crimes
{
	private static readonly Dictionary<string, CrimeType> _crimeDefinitions = InitializeCrimeDefinitions();

	private static Dictionary<string, CrimeType> InitializeCrimeDefinitions()
	{
		Dictionary<string, CrimeType> dictionary = new Dictionary<string, CrimeType>();
		foreach (CrimeType item in Utils.LoadJson<List<CrimeType>>("data/crimes.json"))
		{
			item.Description = Utils.ResolveLocalization(item.Description);
			if (item.MinWantedLevel == 1)
			{
				item.CrimeSeverity = CrimeSeverity.Misdemeanor;
				item.ColorStr = "~y~";
			}
			else if (item.MinWantedLevel >= 2 && item.MinWantedLevel <= 4)
			{
				item.CrimeSeverity = CrimeSeverity.Felony;
				item.ColorStr = "~o~";
			}
			else if (item.MinWantedLevel == 5)
			{
				item.CrimeSeverity = CrimeSeverity.MajorFelony;
				item.ColorStr = "~r~";
			}
			dictionary[item.Id] = item;
		}
		return dictionary;
	}

	public static bool IsDefined(string crimeTypeId)
	{
		return _crimeDefinitions.ContainsKey(crimeTypeId);
	}

	public static CrimeType? GetDefinition(string crimeTypeId)
	{
		return _crimeDefinitions.TryGetRefOrNull(crimeTypeId);
	}

	public static IEnumerable<CrimeType> GetAllDefinitions()
	{
		return _crimeDefinitions.Values;
	}
}
