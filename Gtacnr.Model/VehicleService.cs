using System;

namespace Gtacnr.Model;

public class VehicleService
{
	public int DefaultPrice { get; set; }

	public int MinPrice { get; set; }

	public int MaxPrice { get; set; }

	public float Divisor { get; set; } = 1f;

	public float Multiplier { get; set; } = 1f;

	public int CalculatePrice(int vehicleValue)
	{
		return Math.Max(MinPrice, Math.Min(Convert.ToInt32(Math.Round((float)vehicleValue / Divisor * Multiplier)), MaxPrice));
	}
}
