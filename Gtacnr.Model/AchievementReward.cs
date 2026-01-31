using System.Collections.Generic;

namespace Gtacnr.Model;

public class AchievementReward
{
	public long Money { get; set; }

	public int XP { get; set; }

	public List<string> Items { get; set; } = new List<string>();

	public List<string> Clothes { get; set; } = new List<string>();
}
