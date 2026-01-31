using System.Collections.Generic;

namespace Gtacnr.Model;

public class InventoryEntrySellingData
{
	public List<SellableItemSupply> Supplies { get; set; } = new List<SellableItemSupply>();

	public string Path { get; set; }
}
