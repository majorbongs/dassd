namespace Gtacnr.Client.Vehicles.Fuel;

public class ConsumptionData
{
	public float MinStartValue { get; set; }

	public float MaxStartValue { get; set; }

	public float Multiplier { get; set; }

	public float RPMImpact { get; set; }

	public float AccelerationImpact { get; set; }

	public float TractionImpact { get; set; }
}
