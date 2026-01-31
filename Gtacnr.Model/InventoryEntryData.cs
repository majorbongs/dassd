using Newtonsoft.Json;

namespace Gtacnr.Model;

public class InventoryEntryData
{
	[JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
	public InventoryEntrySellingData Selling { get; set; }

	[JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
	public InventoryEntryWeaponData Weapon { get; set; }

	[JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
	public InventoryEntryDrugData Drug { get; set; }

	public InventoryEntryData()
	{
	}

	public InventoryEntryData(InventoryEntryDataFlags flags)
	{
		if (flags.HasFlag(InventoryEntryDataFlags.Selling))
		{
			Selling = new InventoryEntrySellingData();
		}
		if (flags.HasFlag(InventoryEntryDataFlags.Weapon))
		{
			Weapon = new InventoryEntryWeaponData();
		}
		if (flags.HasFlag(InventoryEntryDataFlags.Drug))
		{
			Drug = new InventoryEntryDrugData();
		}
	}
}
