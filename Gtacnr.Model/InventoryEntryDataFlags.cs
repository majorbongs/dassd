using System;

namespace Gtacnr.Model;

[Flags]
public enum InventoryEntryDataFlags
{
	None = 0,
	Selling = 1,
	Weapon = 2,
	Drug = 4,
	Attachment = 8
}
