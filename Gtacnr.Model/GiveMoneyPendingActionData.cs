namespace Gtacnr.Model;

public class GiveMoneyPendingActionData : PendingActionData
{
	public string CharacterId { get; set; }

	public string Account { get; set; }

	public long Amount { get; set; }

	public string Reason { get; set; }
}
