using System.Collections.Generic;

namespace Gtacnr.Model;

public class AchievementResponse
{
	public Dictionary<string, ulong> Progress { get; set; }

	public Dictionary<string, ulong> UnlockedTiers { get; set; }
}
