using System;

namespace Gtacnr.Model;

public class PersonalVehicleModelDiscount
{
	public string Name { get; set; }

	public DateTime StartDate { get; set; }

	public DateTime EndDate { get; set; }

	public double PercentOff { get; set; }
}
