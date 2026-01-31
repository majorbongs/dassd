using System;

namespace Gtacnr.Model;

public class VehicleRentData
{
	public DateTime StartDateTime { get; set; }

	public DateTime EndDateTime { get; set; }

	public int InitialPrice { get; set; }

	public int RenewPrice { get; set; }
}
