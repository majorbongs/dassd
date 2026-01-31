using CitizenFX.Core;
using CitizenFX.Core.Native;
using Gtacnr.Client.Admin;
using Gtacnr.Client.Vehicles;

namespace Gtacnr.Client.Anticheat;

public class AntiVehicleModifiers : Script
{
	public AntiVehicleModifiers()
	{
		VehicleEvents.EnteringVehicle += ClearVehicleModifiers;
		VehicleEvents.EnteredVehicle += ClearVehicleModifiers;
	}

	private void ClearVehicleModifiers(object sender, VehicleEventArgs e)
	{
		if ((int)StaffLevelScript.StaffLevel <= 0)
		{
			float vehicleGravityAmount = API.GetVehicleGravityAmount(((PoolObject)e.Vehicle).Handle);
			float vehicleCheatPowerIncrease = API.GetVehicleCheatPowerIncrease(((PoolObject)e.Vehicle).Handle);
			float vehicleTopSpeedModifier = API.GetVehicleTopSpeedModifier(((PoolObject)e.Vehicle).Handle);
			if (!IsValidGravity(vehicleGravityAmount))
			{
				API.SetVehicleGravityAmount(((PoolObject)e.Vehicle).Handle, 12f);
			}
			if (!IsValidCheatPower(vehicleCheatPowerIncrease))
			{
				API.SetVehicleCheatPowerIncrease(((PoolObject)e.Vehicle).Handle, 1f);
			}
			if (!IsValidTopSpeed(vehicleTopSpeedModifier))
			{
				API.ModifyVehicleTopSpeed(((PoolObject)e.Vehicle).Handle, 20f);
			}
		}
	}

	private bool IsValidGravity(float gravity)
	{
		if (!(12f >= gravity) || !(gravity >= 9f))
		{
			if (31f >= gravity)
			{
				return gravity >= 29f;
			}
			return false;
		}
		return true;
	}

	private bool IsValidCheatPower(float cheatPower)
	{
		if (1f >= cheatPower)
		{
			return cheatPower >= -2f;
		}
		return false;
	}

	private bool IsValidTopSpeed(float topSpeed)
	{
		if (23.1f >= topSpeed)
		{
			return topSpeed >= -1f;
		}
		return false;
	}

	[Update]
	private async Coroutine VehicleModifiersCheck()
	{
		await Script.Wait(10000);
		if (((Entity)Game.PlayerPed).IsDead)
		{
			return;
		}
		Vehicle currentVehicle = Game.PlayerPed.CurrentVehicle;
		if ((Entity)(object)((currentVehicle != null) ? currentVehicle.Driver : null) != (Entity)(object)Game.PlayerPed || (int)StaffLevelScript.StaffLevel > 0)
		{
			return;
		}
		int handle = ((PoolObject)Game.PlayerPed.CurrentVehicle).Handle;
		if (!API.IsVehicleDriveable(handle, false) || API.NetworkGetEntityOwner(handle) != API.PlayerId())
		{
			return;
		}
		int entityModel = API.GetEntityModel(handle);
		if (!entityModel.In(Constants.Staff.StaffVehicles))
		{
			float vehicleGravityAmount = API.GetVehicleGravityAmount(handle);
			float vehicleCheatPowerIncrease = API.GetVehicleCheatPowerIncrease(handle);
			float vehicleTopSpeedModifier = API.GetVehicleTopSpeedModifier(handle);
			float playerVehicleDefenseModifier = API.GetPlayerVehicleDefenseModifier(API.PlayerId());
			if (!IsValidGravity(vehicleGravityAmount))
			{
				API.DeleteEntity(ref handle);
				SendBanEvent(entityModel, $"Gravity: {vehicleGravityAmount}");
			}
			else if (!IsValidCheatPower(vehicleCheatPowerIncrease))
			{
				API.DeleteEntity(ref handle);
				SendBanEvent(entityModel, $"Cheat power: {vehicleCheatPowerIncrease}");
			}
			else if (!IsValidTopSpeed(vehicleTopSpeedModifier))
			{
				API.DeleteEntity(ref handle);
				SendBanEvent(entityModel, $"Top speed: {vehicleTopSpeedModifier}");
			}
			else if ((double)playerVehicleDefenseModifier > 0.01)
			{
				API.DeleteEntity(ref handle);
				SendLogEvent($"Defense: {playerVehicleDefenseModifier}");
			}
		}
	}

	private void SendBanEvent(int vehModel, string internalInfo)
	{
		string gXTEntry = Game.GetGXTEntry(API.GetDisplayNameFromVehicleModel((uint)vehModel));
		gXTEntry = ((!string.IsNullOrEmpty(gXTEntry)) ? gXTEntry : $"0x{(uint)vehModel:X}");
		internalInfo = internalInfo + ". Vehicle model: " + gXTEntry;
		BaseScript.TriggerServerEvent("gtacnr:ac:banMe", new object[4] { 30, 2, "vehicle modifiers", internalInfo });
	}

	private void SendLogEvent(string internalInfo)
	{
		BaseScript.TriggerServerEvent("gtacnr:ac:logMe", new object[4] { 30, 2, "vehicle modifiers", internalInfo });
	}
}
