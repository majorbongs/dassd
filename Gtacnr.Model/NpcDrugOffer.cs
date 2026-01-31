using System;
using Gtacnr.ResponseCodes;

namespace Gtacnr.Model;

public class NpcDrugOffer
{
	public NpcDrugOfferResponse Response { get; set; }

	public string DrugId { get; set; }

	public float Amount { get; set; }

	public int Price { get; set; }

	public int MaxPrice { get; set; }

	public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
