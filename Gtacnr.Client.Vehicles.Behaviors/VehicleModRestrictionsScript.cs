using System.Collections.Generic;
using System.Linq;
using CitizenFX.Core;
using CitizenFX.Core.Native;

namespace Gtacnr.Client.Vehicles.Behaviors;

public class VehicleModRestrictionsScript : Script
{
	private static Dictionary<int, DisabledModEntry> disabledModEntries = Gtacnr.Utils.LoadJson<List<DisabledModEntry>>("data/vehicles/disabledMods.json").ToDictionary((DisabledModEntry k) => API.GetHashKey(k.VehicleModel), (DisabledModEntry v) => v);

	public static DisabledModEntry GetDisabledModEntry(int vehicleModel)
	{
		if (disabledModEntries.TryGetValue(vehicleModel, out DisabledModEntry value))
		{
			return value;
		}
		return null;
	}

	public VehicleModRestrictionsScript()
	{
		VehicleEvents.EnteredVehicle += OnEnteredVehicle;
	}

	private void OnEnteredVehicle(object sender, VehicleEventArgs e)
	{
		RemoveRestrictedMods(e.Vehicle);
	}

	public static void RemoveRestrictedMods(Vehicle vehicle)
	{
		int handle = ((PoolObject)vehicle).Handle;
		if (!disabledModEntries.TryGetValue(((Entity)vehicle).Model.Hash, out DisabledModEntry value))
		{
			return;
		}
		API.SetVehicleModKit(handle, 0);
		foreach (DisabledModInfo disabledMod in value.DisabledMods)
		{
			if (disabledMod.Index == -1 || API.GetVehicleMod(handle, disabledMod.Type) == disabledMod.Index)
			{
				API.RemoveVehicleMod(handle, disabledMod.Type);
			}
		}
	}
}
