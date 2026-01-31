using System.Collections.Generic;
using CitizenFX.Core;
using Gtacnr.Client.Vehicles;
using Gtacnr.Model;

namespace Gtacnr.Client.Zones;

public sealed class NoVehicleZoneScript : Script
{
	private static readonly List<AABB> noVehicleZones = Gtacnr.Utils.LoadJson<List<AABB>>("data/noVehicleZones.json");

	private static AABB? closestVisibleNoVehicleZone = null;

	private const float VISIBLE_DISTANCE_SQ = 40000f;

	public NoVehicleZoneScript()
	{
		VehicleEvents.EnteredVehicle += OnEnteredVehicle;
		VehicleEvents.LeftVehicle += OnLeftVehicle;
	}

	private void OnEnteredVehicle(object sender, VehicleEventArgs e)
	{
		if (closestVisibleNoVehicleZone != null)
		{
			AttachDrawingTask();
		}
	}

	private void OnLeftVehicle(object sender, VehicleEventArgs e)
	{
		if (closestVisibleNoVehicleZone != null)
		{
			DetachDrawingTask();
		}
	}

	[Update]
	private async Coroutine FindTask()
	{
		await Script.Wait(2000);
		AABB aABB = closestVisibleNoVehicleZone;
		Ped playerPed = Game.PlayerPed;
		Vehicle currentVehicle = playerPed.CurrentVehicle;
		if (!((Entity)(object)playerPed == (Entity)null) && !((Entity)(object)currentVehicle == (Entity)null))
		{
			Vector3 position = ((Entity)playerPed).Position;
			closestVisibleNoVehicleZone = noVehicleZones.Find((AABB z) => z.GetDistanceToSq(position) <= 40000f);
			if (aABB == null && closestVisibleNoVehicleZone != null)
			{
				AttachDrawingTask();
			}
			else if (aABB != null && closestVisibleNoVehicleZone == null)
			{
				DetachDrawingTask();
			}
		}
	}

	private void AttachDrawingTask()
	{
		base.Update += DrawTask;
	}

	private void DetachDrawingTask()
	{
		base.Update -= DrawTask;
	}

	private async Coroutine DrawTask()
	{
		if (closestVisibleNoVehicleZone != null)
		{
			closestVisibleNoVehicleZone.Draw(255, 0, 0, 20);
		}
	}

	[Update]
	private async Coroutine EjectTask()
	{
		await Script.Wait(100);
		if (closestVisibleNoVehicleZone == null)
		{
			return;
		}
		Ped playerPed = Game.PlayerPed;
		Vehicle val = playerPed.CurrentVehicle;
		if (!playerPed.IsInVehicle())
		{
			val = playerPed.LastVehicle;
		}
		if ((Entity)(object)playerPed == (Entity)null || (Entity)(object)val == (Entity)null)
		{
			return;
		}
		Vector3 position = ((Entity)val).Position;
		if (!closestVisibleNoVehicleZone.IsPointInside(position))
		{
			return;
		}
		if (val.PassengerCount > 0)
		{
			Ped[] passengers = val.Passengers;
			for (int i = 0; i < passengers.Length; i++)
			{
				passengers[i].Task.WarpOutOfVehicle(val);
			}
		}
		if (playerPed.IsInVehicle())
		{
			playerPed.Task.WarpOutOfVehicle(val);
			Utils.DisplayHelpText("You can't ~r~use vehicles~s~ in this area.");
			DetachDrawingTask();
		}
		((PoolObject)val).Delete();
	}
}
