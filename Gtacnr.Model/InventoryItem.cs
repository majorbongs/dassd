using System.Collections.Generic;

namespace Gtacnr.Model;

public class InventoryItem : InventoryItemBase
{
	public List<float> UseAmounts { get; set; } = new List<float>();

	public bool IsFractional { get; set; }

	public bool IsIntoxicant { get; set; }

	public bool CanUse { get; set; } = true;

	public bool CanGive { get; set; } = true;

	public bool CanDrop { get; set; } = true;

	public bool EquipWithoutUsing { get; set; }

	public override string ToString()
	{
		return base.Name;
	}
}
