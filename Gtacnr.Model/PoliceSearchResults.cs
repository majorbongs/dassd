using System.Collections.Generic;
using Gtacnr.ResponseCodes;

namespace Gtacnr.Model;

public class PoliceSearchResults
{
	public PoliceSearchResponse ResponseCode { get; set; }

	public List<InventoryEntry> FoundEntries { get; set; } = new List<InventoryEntry>();
}
