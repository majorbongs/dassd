namespace Gtacnr.Client.Estates.Warehouses;

public class WarehouseEventArgs
{
	public string WarehouseId { get; set; }

	public int WarehouseOwnerId { get; set; }

	public WarehouseEventArgs(string warehouseId, int warehouseOwnerId)
	{
		WarehouseId = warehouseId;
		WarehouseOwnerId = warehouseOwnerId;
	}
}
