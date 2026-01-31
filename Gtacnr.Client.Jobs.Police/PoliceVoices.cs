using System.Collections.Generic;
using CitizenFX.Core.Native;
using Gtacnr.Model.Enums;

namespace Gtacnr.Client.Jobs.Police;

public static class PoliceVoices
{
	private static Dictionary<Sex, List<string>> voices;

	public static Dictionary<Sex, List<string>> Voices => voices;

	static PoliceVoices()
	{
		voices = new Dictionary<Sex, List<string>>();
		voices = API.LoadResourceFile(API.GetCurrentResourceName(), "data/police/policeVoices.json").Unjson<Dictionary<Sex, List<string>>>();
	}

	public static string GetVoice(Sex sex, int index)
	{
		if (voices.TryGetValue(sex, out List<string> value) && index >= 0 && index < value.Count)
		{
			return value[index];
		}
		return null;
	}
}
