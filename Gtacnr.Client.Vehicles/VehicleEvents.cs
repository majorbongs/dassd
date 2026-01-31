using System;
using CitizenFX.Core;
using CitizenFX.Core.Native;

namespace Gtacnr.Client.Vehicles;

public class VehicleEvents : Script
{
	private Vehicle lastVehicle;

	private bool isInVehicle;

	private bool isEnteringVehicle;

	public static event EventHandler<VehicleEventArgs> EnteringVehicle;

	public static event EventHandler<VehicleEventArgs> EnteringVehicleAborted;

	public static event EventHandler<VehicleEventArgs> EnteredVehicle;

	public static event EventHandler<VehicleEventArgs> LeftVehicle;

	[Update]
	private async Coroutine VehicleEventsTask()
	{
		await Script.Wait(100);
		if (!isInVehicle && !((Entity)Game.PlayerPed).IsDead)
		{
			if (API.DoesEntityExist(API.GetVehiclePedIsEntering(((PoolObject)Game.PlayerPed).Handle)) && !isEnteringVehicle)
			{
				int vehiclePedIsTryingToEnter = API.GetVehiclePedIsTryingToEnter(((PoolObject)Game.PlayerPed).Handle);
				int seatPedIsTryingToEnter = API.GetSeatPedIsTryingToEnter(((PoolObject)Game.PlayerPed).Handle);
				isEnteringVehicle = true;
				lastVehicle = new Vehicle(vehiclePedIsTryingToEnter);
				VehicleEvents.EnteringVehicle?.Invoke(this, new VehicleEventArgs(lastVehicle, (VehicleSeat)seatPedIsTryingToEnter));
			}
			else if (!API.DoesEntityExist(API.GetVehiclePedIsEntering(((PoolObject)Game.PlayerPed).Handle)) && !API.IsPedInAnyVehicle(((PoolObject)Game.PlayerPed).Handle, true) && isEnteringVehicle)
			{
				isEnteringVehicle = false;
				VehicleEvents.EnteringVehicleAborted?.Invoke(this, new VehicleEventArgs(lastVehicle));
			}
			else if (API.IsPedInAnyVehicle(((PoolObject)Game.PlayerPed).Handle, false))
			{
				isEnteringVehicle = false;
				isInVehicle = true;
				lastVehicle = Game.PlayerPed.CurrentVehicle;
				VehicleSeat val = (VehicleSeat)API.GetSeatPedIsTryingToEnter(((PoolObject)Game.PlayerPed).Handle);
				if ((int)val != -3)
				{
					VehicleEvents.EnteredVehicle?.Invoke(this, new VehicleEventArgs(lastVehicle, val));
				}
				else
				{
					VehicleEvents.EnteredVehicle?.Invoke(this, new VehicleEventArgs(lastVehicle, Game.PlayerPed.SeatIndex));
				}
				Utils.RemoveAllAttachedProps();
			}
		}
		else if (isInVehicle && (!API.IsPedInAnyVehicle(((PoolObject)Game.PlayerPed).Handle, false) || ((Entity)Game.PlayerPed).IsDead))
		{
			isInVehicle = false;
			VehicleEvents.LeftVehicle?.Invoke(this, new VehicleEventArgs(lastVehicle));
		}
	}
}
