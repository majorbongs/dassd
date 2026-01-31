namespace Gtacnr.Model;

public sealed class CrewLogAcronymStyleChangedData : ICrewLogData
{
	public AcronymStyle Previous { get; set; }

	public AcronymStyle New { get; set; }
}
