using System.Collections.Generic;

namespace Gtacnr.Model;

public class PunishmentData
{
	public string Text { get; set; }

	public int Amount { get; set; }

	public string Job { get; set; }

	public List<PunishmentHistoryData> History { get; set; }
}
