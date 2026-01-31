using System.Collections.Generic;

namespace Gtacnr.Model;

public class InventoryEntryWeaponData
{
	public bool IsEquipped { get; set; }

	public int TintIndex { get; set; }

	public List<string> Components { get; set; } = new List<string>();
}
