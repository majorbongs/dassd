using System;
using CitizenFX.Core;

namespace Gtacnr.Client.Vehicles;

public class VehicleEventArgs : EventArgs
{
	public Vehicle Vehicle { get; set; }

	public VehicleSeat Seat { get; set; } = (VehicleSeat)(-3);

	public VehicleEventArgs(Vehicle vehicle, VehicleSeat seat)
	{
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		Vehicle = vehicle;
		Seat = seat;
	}

	public VehicleEventArgs(Vehicle vehicle)
		: this(vehicle, (VehicleSeat)(-3))
	{
	}
}
