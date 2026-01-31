using System.Collections.Generic;
using CitizenFX.Core;
using CitizenFX.Core.Native;

namespace Gtacnr.Client.Anticheat;

public class VehicleBlacklistScript : Script
{
	private HashSet<int> vehiclesToRemove = new HashSet<int>
	{
		API.GetHashKey("blimp"),
		API.GetHashKey("blimp2"),
		API.GetHashKey("cargoplane"),
		API.GetHashKey("hunter")
	};

	[Update]
	private async Coroutine VehiclesToRemoveTick()
	{
		await Script.Wait(3000);
		Vehicle[] allVehicles = World.GetAllVehicles();
		foreach (Vehicle val in allVehicles)
		{
			int hash = ((Entity)val).Model.Hash;
			if (!vehiclesToRemove.Contains(hash))
			{
				continue;
			}
			if ((Entity)(object)val.Driver != (Entity)null && !val.Driver.IsPlayer)
			{
				((PoolObject)val.Driver).Delete();
			}
			Ped[] passengers = val.Passengers;
			foreach (Ped val2 in passengers)
			{
				if (!val2.IsPlayer)
				{
					((PoolObject)val2).Delete();
				}
			}
			((PoolObject)val).Delete();
			await Script.Yield();
		}
	}
}
