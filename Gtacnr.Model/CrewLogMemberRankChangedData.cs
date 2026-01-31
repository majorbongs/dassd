namespace Gtacnr.Model;

public sealed class CrewLogMemberRankChangedData : ICrewLogData
{
	public int Previous { get; set; }

	public int New { get; set; }
}
