using System.Collections.Generic;
using Gtacnr.Model.Enums;

namespace Gtacnr.Model;

public class Inventory
{
	public string Id { get; set; }

	public InventoryType Type { get; set; }

	public string Owner { get; set; }

	public string Job { get; set; }

	public dynamic Metadata { get; set; }

	public List<InventoryEntry> Content { get; set; }
}
