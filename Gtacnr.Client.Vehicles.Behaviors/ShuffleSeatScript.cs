using System;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Gtacnr.Client.API.UI;

namespace Gtacnr.Client.Vehicles.Behaviors;

public class ShuffleSeatScript : Script
{
	private bool instructionsShown;

	public static EventHandler<VehicleEventArgs> SeatShuffled;

	[Update]
	private async Coroutine DisableAutoShuffleTask()
	{
		Vehicle vehicle = Game.PlayerPed.CurrentVehicle;
		if ((Entity)(object)vehicle == (Entity)null)
		{
			DisableInstructionalButtons();
		}
		else if (Game.IsControlPressed(2, (Control)71) && IsDriverSeatFree())
		{
			Utils.SetPedConfigFlagEx(Game.PlayerPed, PedConfigFlag.PreventAutoShuffleToDriversSeat, value: false);
			DisableInstructionalButtons();
			SeatShuffled?.Invoke(this, new VehicleEventArgs(vehicle, (VehicleSeat)(-1)));
			await Script.Wait(1000);
		}
		else
		{
			Utils.SetPedConfigFlagEx(Game.PlayerPed, PedConfigFlag.PreventAutoShuffleToDriversSeat, value: true);
			if (IsDriverSeatFree())
			{
				EnableInstructionalButtons();
			}
			else
			{
				DisableInstructionalButtons();
			}
		}
		bool IsDriverSeatFree()
		{
			if (API.GetPedInVehicleSeat(((PoolObject)vehicle).Handle, 0) == ((PoolObject)Game.PlayerPed).Handle)
			{
				return API.GetPedInVehicleSeat(((PoolObject)vehicle).Handle, -1) <= 0;
			}
			return false;
		}
	}

	private void EnableInstructionalButtons()
	{
		if (!instructionsShown)
		{
			instructionsShown = true;
			Utils.AddInstructionalButton("shuffle", new InstructionalButton("Drive", 2, (Control)71));
		}
	}

	private void DisableInstructionalButtons()
	{
		if (instructionsShown)
		{
			instructionsShown = false;
			Utils.RemoveInstructionalButton("shuffle");
		}
	}
}
