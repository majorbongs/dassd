namespace Gtacnr.Model;

public class PunishmentHistoryData
{
	public string UserId { get; set; }

	public PunishmentEditType Type { get; set; }

	public string Reason { get; set; }

	public int PreviousAmount { get; set; }
}
