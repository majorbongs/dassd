using System.Collections.Generic;
using System.Linq;
using Gtacnr.Model;

namespace Gtacnr.Data;

public static class DailyChallenges
{
	private static readonly Dictionary<string, DailyChallenge> _challengesDefinitions = InitializeChallengeDefinitions();

	private static Dictionary<string, DailyChallenge> InitializeChallengeDefinitions()
	{
		Dictionary<string, DailyChallenge> dictionary = new Dictionary<string, DailyChallenge>();
		foreach (DailyChallenge item in Utils.LoadJson<List<DailyChallenge>>("data/dailyChallenges.json"))
		{
			dictionary[item.Id] = item;
		}
		return dictionary;
	}

	public static DailyChallenge? GetChallengeDefinition(string challengeId)
	{
		if (_challengesDefinitions.ContainsKey(challengeId))
		{
			return _challengesDefinitions[challengeId];
		}
		return null;
	}

	public static IEnumerable<DailyChallenge> GetAllChallengeDefinitions()
	{
		return _challengesDefinitions.Values.AsEnumerable();
	}
}
