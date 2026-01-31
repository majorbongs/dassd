using CitizenFX.Core;

namespace Gtacnr.Model;

public class DropInfo
{
	public string Id { get; set; }

	public int PlayerId { get; set; }

	public string UserId { get; set; }

	public string CharId { get; set; }

	public InventoryEntry Entry { get; set; }

	public Vector3 Coords { get; set; }

	public int PropId { get; set; }

	public bool PickUpInProgress { get; set; }
}
