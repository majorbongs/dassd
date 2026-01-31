using System.Collections.Generic;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Gtacnr.Client.Admin;
using Gtacnr.Client.API;
using Gtacnr.Client.Vehicles;

namespace Gtacnr.Client.Zones;

public class NoFlyZoneScript : Script
{
	private class NoFlyZone
	{
		private const float VISIBILITY_RANGE = 1000f;

		public float[] Min_ { get; set; }

		public float[] Max_ { get; set; }

		public float Altitude { get; set; }

		public Vector2 Min => new Vector2(Min_[0], Min_[1]);

		public Vector2 Max => new Vector2(Max_[0], Max_[1]);

		public bool IsPointWithinZone(Vector3 point)
		{
			//IL_0000: Unknown result type (might be due to invalid IL or missing references)
			//IL_0007: Unknown result type (might be due to invalid IL or missing references)
			//IL_0013: Unknown result type (might be due to invalid IL or missing references)
			//IL_001a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0052: Unknown result type (might be due to invalid IL or missing references)
			//IL_0026: Unknown result type (might be due to invalid IL or missing references)
			//IL_002d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0039: Unknown result type (might be due to invalid IL or missing references)
			//IL_0040: Unknown result type (might be due to invalid IL or missing references)
			bool num = point.X >= Min.X && point.X <= Max.X && point.Y >= Min.Y && point.Y <= Max.Y;
			bool flag = point.Z <= Altitude;
			return num && flag;
		}

		public bool IsZoneVisible(Vector3 position)
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			//IL_0011: Unknown result type (might be due to invalid IL or missing references)
			//IL_0019: Unknown result type (might be due to invalid IL or missing references)
			//IL_0020: Unknown result type (might be due to invalid IL or missing references)
			//IL_0033: Unknown result type (might be due to invalid IL or missing references)
			//IL_0043: Unknown result type (might be due to invalid IL or missing references)
			//IL_004b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0052: Unknown result type (might be due to invalid IL or missing references)
			if (Min.X - 1000f < position.X && position.X < Max.X + 1000f)
			{
				if (Min.Y - 1000f < position.Y)
				{
					return position.Y < Max.Y + 1000f;
				}
				return false;
			}
			return false;
		}
	}

	private static readonly HashSet<int> ignoredModels = new HashSet<int>
	{
		API.GetHashKey("sled"),
		API.GetHashKey("cv22")
	};

	private readonly List<NoFlyZone> noFlyZones = Gtacnr.Utils.LoadJson<List<NoFlyZone>>("data/noFlyZones.json");

	private NoFlyZone closestVisibleNoFlyZone;

	private bool disabled;

	public NoFlyZoneScript()
	{
		VehicleEvents.EnteredVehicle += OnEnteredVehicle;
		VehicleEvents.LeftVehicle += OnLeftVehicle;
	}

	private bool IsVehicleAffected(Vehicle vehicle)
	{
		if (((Entity)vehicle).Model.IsPlane)
		{
			return !ignoredModels.Contains(Model.op_Implicit(((Entity)vehicle).Model));
		}
		return false;
	}

	private void OnEnteredVehicle(object sender, VehicleEventArgs e)
	{
		if (IsVehicleAffected(e.Vehicle) && closestVisibleNoFlyZone != null)
		{
			AttachDrawingTask();
		}
	}

	private void OnLeftVehicle(object sender, VehicleEventArgs e)
	{
		if (IsVehicleAffected(e.Vehicle) && closestVisibleNoFlyZone != null)
		{
			DetachDrawingTask();
		}
	}

	[Update]
	private async Coroutine EjectTask()
	{
		await Script.Wait(100);
		if (disabled || closestVisibleNoFlyZone == null)
		{
			return;
		}
		Ped playerPed = Game.PlayerPed;
		Vehicle val = playerPed.CurrentVehicle;
		if (!playerPed.IsInVehicle())
		{
			val = playerPed.LastVehicle;
		}
		if ((Entity)(object)playerPed == (Entity)null || (Entity)(object)val == (Entity)null || !IsVehicleAffected(val))
		{
			return;
		}
		Vector3 position = ((Entity)val).Position;
		if (!closestVisibleNoFlyZone.IsPointWithinZone(position))
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
			((Entity)playerPed).Position = new Vector3(((Entity)playerPed).Position.X, ((Entity)playerPed).Position.Y, ((Entity)playerPed).Position.Z + 50f);
			((Entity)playerPed).IsPositionFrozen = false;
			playerPed.Weapons.Give((WeaponHash)(-72657034), 1, true, true);
			Utils.DisplayHelpText("You can't ~r~fly airplanes ~s~at low altitude in this area.");
			DetachDrawingTask();
		}
		((PoolObject)val).Delete();
	}

	[Update]
	private async Coroutine FindTask()
	{
		await Script.Wait(2000);
		if (disabled)
		{
			return;
		}
		NoFlyZone noFlyZone = closestVisibleNoFlyZone;
		Ped playerPed = Game.PlayerPed;
		Vehicle currentVehicle = playerPed.CurrentVehicle;
		if (!((Entity)(object)playerPed == (Entity)null) && !((Entity)(object)currentVehicle == (Entity)null) && IsVehicleAffected(currentVehicle))
		{
			Vector3 position = ((Entity)playerPed).Position;
			closestVisibleNoFlyZone = noFlyZones.Find((NoFlyZone z) => z.IsZoneVisible(position));
			if (noFlyZone == null && closestVisibleNoFlyZone != null)
			{
				AttachDrawingTask();
			}
			else if (noFlyZone != null && closestVisibleNoFlyZone == null)
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
		if (closestVisibleNoFlyZone != null)
		{
			Vector2 min = closestVisibleNoFlyZone.Min;
			Vector2 max = closestVisibleNoFlyZone.Max;
			API.DrawBox(min.X, min.Y, 0f, max.X, max.Y, closestVisibleNoFlyZone.Altitude, 255, 0, 0, 20);
		}
	}

	[Command("nofly-zones")]
	private void NoFlyZonesCommand()
	{
		if ((int)StaffLevelScript.StaffLevel < 100)
		{
			Chat.AddMessage(Gtacnr.Utils.Colors.Error, "You are not authorized to use this command.");
			return;
		}
		disabled = !disabled;
		if (disabled)
		{
			closestVisibleNoFlyZone = null;
			DetachDrawingTask();
		}
		Chat.AddMessage(Gtacnr.Utils.Colors.Info, "No-fly zones have been " + (disabled ? "disabled" : "enabled") + " for you.");
	}
}
