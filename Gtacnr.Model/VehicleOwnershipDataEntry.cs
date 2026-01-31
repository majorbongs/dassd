using System;

namespace Gtacnr.Model;

public class VehicleOwnershipDataEntry
{
	public string OwnerCharacterId { get; set; }

	public DateTime? PurchaseDateTime { get; set; }

	public int PurchasePrice { get; set; }
}
