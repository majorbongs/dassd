using System.Collections.Generic;

namespace Gtacnr.Client.Vehicles.Behaviors;

public class DisabledModEntry
{
	public string VehicleModel { get; set; }

	public List<DisabledModInfo> DisabledMods { get; set; }
}
