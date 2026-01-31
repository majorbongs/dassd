using System;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Gtacnr.Data;
using Gtacnr.Localization;
using Gtacnr.Model;

namespace Gtacnr.Client.Items;

public class RepairKitScript : Script
{
	private readonly TimeSpan ENGINE_KIT_COOLDOWN = TimeSpan.FromMilliseconds(30000.0);

	private readonly TimeSpan TIRE_KIT_COOLDOWN = TimeSpan.FromMilliseconds(5000.0);

	private readonly TimeSpan CLEANING_KIT_COOLDOWN = TimeSpan.FromMilliseconds(5000.0);

	private DateTime engineKitUseTimestamp;

	private DateTime tireKitUseTimestamp;

	private DateTime cleaningKitUseTimestamp;

	[EventHandler("gtacnr:inventories:usingItem")]
	private void OnUsingItem(string itemId, float amount)
	{
		InventoryItem itemInfo = Gtacnr.Data.Items.GetItemDefinition(itemId);
		if (itemInfo == null)
		{
			return;
		}
		Vehicle vehicle = Game.PlayerPed.LastVehicle;
		if (itemId == "engine_kit")
		{
			bool flag = Checks(engineKitUseTimestamp, ENGINE_KIT_COOLDOWN);
			if (flag && vehicle.EngineHealth >= 750f)
			{
				Utils.SendNotification(LocalizationController.S(Entries.Specialitems.SPECIALITEM_REPAIRKIT_NOT_DAMAGED, itemInfo.Name));
				flag = false;
			}
			if (!flag)
			{
				API.CancelEvent();
			}
		}
		else if (itemId == "tire_kit")
		{
			if (!Checks(tireKitUseTimestamp, TIRE_KIT_COOLDOWN))
			{
				API.CancelEvent();
			}
		}
		else if (itemId == "cleaning_kit" && !Checks(cleaningKitUseTimestamp, CLEANING_KIT_COOLDOWN, checkClass: false))
		{
			API.CancelEvent();
		}
		bool Checks(DateTime usageTimestamp, TimeSpan cooldown, bool checkClass = true)
		{
			//IL_006e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0073: Unknown result type (might be due to invalid IL or missing references)
			//IL_007b: Unknown result type (might be due to invalid IL or missing references)
			//IL_010d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0112: Unknown result type (might be due to invalid IL or missing references)
			//IL_0113: Unknown result type (might be due to invalid IL or missing references)
			//IL_0116: Invalid comparison between Unknown and I4
			//IL_0118: Unknown result type (might be due to invalid IL or missing references)
			//IL_011b: Invalid comparison between Unknown and I4
			//IL_011d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0120: Invalid comparison between Unknown and I4
			//IL_0122: Unknown result type (might be due to invalid IL or missing references)
			//IL_0125: Invalid comparison between Unknown and I4
			if ((Entity)(object)vehicle == (Entity)null)
			{
				Utils.SendNotification(LocalizationController.S(Entries.Specialitems.SPECIALITEM_REPAIRKIT_VEHICLE_MISSING));
				return false;
			}
			if (Game.PlayerPed.IsInVehicle())
			{
				Utils.SendNotification(LocalizationController.S(Entries.Specialitems.SPECIALITEM_REPAIRKIT_VEHICLE_INSIDE, itemInfo.Name));
				return false;
			}
			Vector3 position = ((Entity)vehicle).Position;
			if (!(((Vector3)(ref position)).DistanceToSquared(((Entity)Game.PlayerPed).Position) < 100f))
			{
				Utils.SendNotification(LocalizationController.S(Entries.Specialitems.SPECIALITEM_REPAIRKIT_VEHICLE_CLOSE, itemInfo.Name));
				return false;
			}
			if (!API.NetworkHasControlOfEntity(((PoolObject)vehicle).Handle) && !API.NetworkRequestControlOfEntity(((PoolObject)vehicle).Handle))
			{
				Utils.SendNotification(LocalizationController.S(Entries.Specialitems.SPECIALITEM_REPAIRKIT_VEHICLE_CONTROL));
				return false;
			}
			if (checkClass)
			{
				VehicleClass classType = Game.PlayerPed.LastVehicle.ClassType;
				if ((int)classType == 14 || (int)classType == 16 || (int)classType == 15 || (int)classType == 19)
				{
					Utils.SendNotification(LocalizationController.S(Entries.Specialitems.SPECIALITEM_REPAIRKIT_VEHICLE_TYPE, itemInfo.Name));
					return false;
				}
			}
			if (itemId == "engine_kit" && vehicle.Doors.HasDoor((VehicleDoorIndex)4) && !vehicle.Doors[(VehicleDoorIndex)4].IsOpen)
			{
				Utils.SendNotification(LocalizationController.S(Entries.Specialitems.SPECIALITEM_REPAIRKIT_VEHICLE_HOOD, itemInfo.Name));
				return false;
			}
			if (!Gtacnr.Utils.CheckTimePassed(usageTimestamp, cooldown))
			{
				TimeSpan timeSpan = cooldown - (DateTime.UtcNow - usageTimestamp);
				Utils.SendNotification(LocalizationController.S(Entries.Specialitems.SPECIALITEM_REPAIRKIT_COOLDOWN, $"{timeSpan.TotalSeconds:0}", itemInfo.Name));
				return false;
			}
			return true;
		}
	}

	[EventHandler("gtacnr:inventories:usedItem")]
	private void OnUsedItem(string itemId, float amount)
	{
		switch (itemId)
		{
		case "engine_kit":
		{
			engineKitUseTimestamp = DateTime.UtcNow;
			Vehicle lastVehicle2 = Game.PlayerPed.LastVehicle;
			if ((Entity)(object)lastVehicle2 != (Entity)null)
			{
				lastVehicle2.EngineHealth = 750f;
				Utils.SendNotification(LocalizationController.S(Entries.Specialitems.SPECIALITEM_REPAIRKIT_ENGINE_REPAIR));
				if (lastVehicle2.Doors.HasDoor((VehicleDoorIndex)4))
				{
					lastVehicle2.Doors[(VehicleDoorIndex)4].Close(false);
				}
			}
			break;
		}
		case "tire_kit":
		{
			tireKitUseTimestamp = DateTime.UtcNow;
			Vehicle lastVehicle3 = Game.PlayerPed.LastVehicle;
			if (!((Entity)(object)lastVehicle3 != (Entity)null))
			{
				break;
			}
			for (int i = 0; i < 16; i++)
			{
				try
				{
					API.SetVehicleTyreFixed(((PoolObject)lastVehicle3).Handle, i);
					API.SetVehicleWheelHealth(((PoolObject)lastVehicle3).Handle, i, 1000f);
				}
				catch (Exception exception)
				{
					Print(exception);
					break;
				}
			}
			Utils.SendNotification("You have repaired your ~b~vehicle~s~'s tires.");
			break;
		}
		case "cleaning_kit":
		{
			cleaningKitUseTimestamp = DateTime.UtcNow;
			Vehicle lastVehicle = Game.PlayerPed.LastVehicle;
			if ((Entity)(object)lastVehicle != (Entity)null)
			{
				lastVehicle.DirtLevel = 0f;
				API.RemoveDecalsFromVehicle(((PoolObject)lastVehicle).Handle);
				Utils.SendNotification("You have cleaned your ~b~vehicle~s~.");
			}
			break;
		}
		}
	}
}
