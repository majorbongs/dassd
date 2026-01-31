using System.Collections.Generic;

namespace Gtacnr.Client.Vehicles.Fuel;

public class GasConfig
{
	public GasConfigBehavior Behavior { get; set; }

	public HashSet<string> IgnoredVehicles { get; set; }

	public HashSet<string> IgnoredClasses { get; set; }
}
