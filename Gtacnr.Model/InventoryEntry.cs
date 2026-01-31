namespace Gtacnr.Model;

public class InventoryEntry
{
	public string? InventoryId { get; set; }

	public string? ItemId { get; set; }

	public float Amount { get; set; }

	public int Position { get; set; }

	public InventoryEntryData? Data { get; set; }

	public string FormatAmount(string unit)
	{
		return string.Format("{0:0.##}{1}{2}{3}", Amount, (unit != null && unit.Length > 2) ? " " : "", unit ?? " piece", (Amount == 1f || (unit != null && unit.Length <= 2)) ? "" : "s");
	}

	public InventoryEntry()
	{
	}

	public InventoryEntry(string itemId, float amount)
	{
		ItemId = itemId;
		Amount = amount;
	}

	public InventoryEntry(string itemId)
	{
		ItemId = itemId;
		Amount = 1f;
	}
}
