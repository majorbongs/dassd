using System.Collections.Generic;
using Gtacnr.ResponseCodes;

namespace Gtacnr.Model;

public class EnterExitPropertyWithVehicleData
{
	public string GarageId { get; set; }

	public int GarageOwnerId { get; set; }

	public List<int> Passengers { get; set; }

	public EnterExitPropertyResponse ResponseCode { get; set; }

	public StoredVehicle StoredVehicle { get; set; }

	public int ParkIndex { get; set; }
}
