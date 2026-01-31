namespace Gtacnr.Model;

public sealed class CrewLogAcronymSeparatorChangedData : ICrewLogData
{
	public AcronymStyleSeparator Previous { get; set; }

	public AcronymStyleSeparator New { get; set; }
}
