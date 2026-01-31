using System.Collections.Generic;
using Gtacnr.Model;
using Gtacnr.Model.Enums;

namespace Gtacnr;

public interface IInventory
{
	string Id { get; set; }

	InventoryType Type { get; set; }

	string? Parameter { get; set; }

	IEnumerable<InventoryEntry> Entries { get; set; }
}
