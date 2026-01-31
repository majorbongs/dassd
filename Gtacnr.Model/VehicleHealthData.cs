namespace Gtacnr.Model;

public class VehicleHealthData
{
	public float EngineHealth { get; set; } = 1000f;

	public float BodyHealth { get; set; } = 1000f;

	public float PetrolTankHealth { get; set; } = 1000f;

	public float[] WheelHealth { get; set; }

	public float DirtLevel { get; set; }

	public float Fuel { get; set; } = 0.4f;
}
