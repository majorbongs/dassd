using System.Collections.Generic;

namespace Gtacnr.Client.Vehicles.Fuel;

public class GasConfigBehavior
{
	public int Leak { get; set; }

	public Dictionary<string, ConsumptionData> Modifiers { get; set; }
}
