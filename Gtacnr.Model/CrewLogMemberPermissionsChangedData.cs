namespace Gtacnr.Model;

public sealed class CrewLogMemberPermissionsChangedData : ICrewLogData
{
	public CrewPermissions Previous { get; set; }

	public CrewPermissions New { get; set; }
}
