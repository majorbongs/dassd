namespace Gtacnr.Model;

public class SellableItemSupply
{
	public float Amount { get; set; }

	public int Price { get; set; }

	public string FormatAmount(string unit)
	{
		return new InventoryEntry("", Amount).FormatAmount(unit);
	}
}
