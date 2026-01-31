using System.Collections.Generic;

namespace Gtacnr.Model;

public class TradeOffer
{
	public ulong Cash { get; set; }

	public List<InventoryEntry> Items { get; set; }

	public List<PurchaseableEntry> PurchaseableItems { get; set; }

	public TradeOffer()
	{
		Cash = 0uL;
		Items = new List<InventoryEntry>();
		PurchaseableItems = new List<PurchaseableEntry>();
	}
}
