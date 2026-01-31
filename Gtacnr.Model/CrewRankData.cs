using System.Collections.Generic;

namespace Gtacnr.Model;

public class CrewRankData
{
	public Dictionary<int, string> RankNames { get; set; } = new Dictionary<int, string>();

	public static string GetDefaultRankName(int rankId)
	{
		return rankId switch
		{
			0 => "Recruit", 
			1 => "Soldier", 
			2 => "Sergeant", 
			3 => "Lieutenant", 
			4 => "Captain", 
			99 => "Owner", 
			_ => $"Rank {rankId}", 
		};
	}
}
