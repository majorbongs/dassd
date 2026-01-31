using Gtacnr.Model.Enums;

namespace Gtacnr.Model;

public class InventoryEntryArgs
{
	public string ownerId;

	public InventoryType type;

	public InventoryEntry entry;

	public string job;

	public InventoryEntryArgs(string ownerId, InventoryType type, InventoryEntry entry, string job = null)
	{
		this.ownerId = ownerId;
		this.type = type;
		this.entry = entry;
		this.job = job;
	}
}
