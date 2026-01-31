using CitizenFX.Core;
using CitizenFX.Core.Native;

namespace Gtacnr.Client.Vehicles.Behaviors;

public class ForceVehicleNetworkingScript : Script
{
	[Update]
	private async Coroutine ForceNetworkingTask()
	{
		await Script.Wait(1000);
		Vehicle currentVehicle = Game.PlayerPed.CurrentVehicle;
		if ((Entity)(object)currentVehicle != (Entity)null && currentVehicle.Exists())
		{
			API.NetworkRegisterEntityAsNetworked(((PoolObject)currentVehicle).Handle);
		}
	}
}
