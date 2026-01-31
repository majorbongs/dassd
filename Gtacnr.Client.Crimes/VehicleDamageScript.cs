using System;
using System.Collections.Generic;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Gtacnr.Client.API;
using Gtacnr.Client.Jobs.Mechanic;
using Gtacnr.Client.Vehicles;

namespace Gtacnr.Client.Crimes;

public class VehicleDamageScript : Script
{
	private static Dictionary<int, DateTime> damagedVehiclesList = new Dictionary<int, DateTime>();

	private static VehicleDamageScript instance;

	public VehicleDamageScript()
	{
		instance = this;
	}

	public static void OnDamageVehicle(Vehicle vehicle, int weapon)
	{
		try
		{
			if (!Gtacnr.Client.API.Jobs.CachedJobEnum.IsPublicService() && API.NetworkGetEntityIsNetworked(((PoolObject)vehicle).Handle) && ((Entity)vehicle).NetworkId != 0)
			{
				bool flag = weapon == -1553120962 || weapon == 133987706;
				bool flag2 = API.GetWeaponDamageType((uint)weapon) == 2;
				bool flag3 = API.GetWeaponDamageType((uint)weapon) == 3;
				bool num = Gtacnr.Utils.IsVehicleModelAPoliceVehicle(Model.op_Implicit(((Entity)vehicle).Model));
				bool flag4 = Gtacnr.Utils.IsVehicleModelAParamedicVehicle(Model.op_Implicit(((Entity)vehicle).Model));
				bool flag5 = num || flag4;
				int activeVehicleNetId = ActiveVehicleScript.ActiveVehicleNetId;
				Vehicle currentVehicle = Game.PlayerPed.CurrentVehicle;
				bool num2 = activeVehicleNetId != 0 && activeVehicleNetId == ((Entity)vehicle).NetworkId;
				bool flag6 = (Entity)(object)currentVehicle != (Entity)null && TowingScript.IsTowTruck((VehicleHash)((Entity)currentVehicle).Model.Hash);
				string text = (num2 ? null : ((!flag) ? ((!flag2) ? ((!flag3) ? null : (flag5 ? "shoot_emergency_vehicle" : "shoot_vehicle")) : (flag5 ? "damage_emergency_vehicle" : null)) : (flag6 ? null : (flag5 ? "damage_emergency_vehicle" : null))));
				if (!damagedVehiclesList.ContainsKey(((Entity)vehicle).NetworkId))
				{
					damagedVehiclesList[((Entity)vehicle).NetworkId] = default(DateTime);
				}
				if (Gtacnr.Utils.CheckTimePassed(damagedVehiclesList[((Entity)vehicle).NetworkId], TimeSpan.FromMinutes(2.0)) && text != null)
				{
					BaseScript.TriggerServerEvent("gtacnr:crimes:damagedVehicle", new object[2]
					{
						((Entity)vehicle).NetworkId,
						text
					});
					damagedVehiclesList[((Entity)vehicle).NetworkId] = DateTime.UtcNow;
				}
			}
		}
		catch (Exception exception)
		{
			instance.Print(exception);
		}
	}
}
