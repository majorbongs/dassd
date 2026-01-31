using Gtacnr.Model.PrefixedGUIDs;

namespace Gtacnr.Model;

public class CrewMemberData
{
	public string UserId { get; set; }

	public CrewId CrewId { get; set; }

	public int Rank { get; set; }

	public CrewPermissions Permissions { get; set; }

	public string Username { get; set; }

	public string GetRankName(Crew crewData)
	{
		if (crewData == null)
		{
			return "Undefined";
		}
		if (crewData.RankData != null && crewData.RankData.RankNames.ContainsKey(Rank))
		{
			return crewData.RankData.RankNames[Rank];
		}
		return CrewRankData.GetDefaultRankName(Rank);
	}
}
