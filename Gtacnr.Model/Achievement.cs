using System.Collections.Generic;

namespace Gtacnr.Model;

public class Achievement
{
	public string Id { get; set; }

	public string Name { get; set; }

	public bool IsSecret { get; set; }

	public Dictionary<ulong, AchievementTier> Tiers { get; set; }
}
