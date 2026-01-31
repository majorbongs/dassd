using System.Collections.Generic;

namespace Gtacnr.Model;

public class VehicleOwnershipData
{
	public List<VehicleOwnershipDataEntry> History { get; set; } = new List<VehicleOwnershipDataEntry>();
}
