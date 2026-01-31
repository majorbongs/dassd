using System.Collections.Generic;

namespace Gtacnr.Model;

public class SellerItemList
{
	public List<InventoryEntry> Entries { get; set; } = new List<InventoryEntry>();

	public ServiceData Services { get; set; } = new ServiceData();
}
