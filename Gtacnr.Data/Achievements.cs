using System.Collections.Generic;
using System.Linq;
using Gtacnr.Model;

namespace Gtacnr.Data;

public static class Achievements
{
	private static readonly Dictionary<string, Achievement> _achievementsDefinitions = InitializeAchievementDefinitions();

	private static Dictionary<string, Achievement> InitializeAchievementDefinitions()
	{
		Dictionary<string, Achievement> dictionary = new Dictionary<string, Achievement>();
		foreach (Achievement item in Utils.LoadJson<List<Achievement>>("data/achievements.json"))
		{
			dictionary[item.Id] = item;
		}
		return dictionary;
	}

	public static Achievement? GetAchievementDefinition(string achievementId)
	{
		if (_achievementsDefinitions.ContainsKey(achievementId))
		{
			return _achievementsDefinitions[achievementId];
		}
		return null;
	}

	public static IEnumerable<Achievement> GetAllAchievementDefinitions()
	{
		return _achievementsDefinitions.Values.AsEnumerable();
	}
}
