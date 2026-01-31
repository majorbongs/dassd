using System.Collections.Generic;
using Gtacnr.Model;

namespace Gtacnr.Client.Crimes.Robberies.Jewelry;

public class ItemsTakenEventArgs
{
	public List<InventoryEntry> Entries { get; set; }

	public ItemsTakenEventArgs(List<InventoryEntry> entries)
	{
		Entries = entries;
	}
}
