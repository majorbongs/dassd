using CitizenFX.Core;
using Gtacnr.Client.API;
using Gtacnr.Client.Jobs.Police.Arrest;
using Gtacnr.Client.Jobs.Trucker;
using Gtacnr.Model.Enums;

namespace Gtacnr.Client.Jobs.Police;

public class UnlockPoliceVehiclesScript : Script
{
	[Update]
	private async Coroutine UnlockPoliceVehiclesTask()
	{
		await Script.Wait(250);
		JobsEnum cachedJobEnum = Gtacnr.Client.API.Jobs.CachedJobEnum;
		Vehicle vehicleTryingToEnter = Game.PlayerPed.VehicleTryingToEnter;
		if ((Entity)(object)vehicleTryingToEnter == (Entity)null || cachedJobEnum == JobsEnum.Invalid || !string.IsNullOrEmpty(LatentVehicleStateScript.Get(((Entity)vehicleTryingToEnter).NetworkId)?.PersonalVehicleId))
		{
			return;
		}
		if ((dynamic)((Entity)vehicleTryingToEnter).State.Get("gtacnr:isTransportUnit") == true)
		{
			SetLocked(vehicleTryingToEnter, isLocked: true, canEnter: false);
			return;
		}
		bool flag = Gtacnr.Utils.IsVehicleModelAPoliceVehicle(Model.op_Implicit(((Entity)vehicleTryingToEnter).Model));
		bool flag2 = Gtacnr.Utils.IsVehicleModelAParamedicVehicle(Model.op_Implicit(((Entity)vehicleTryingToEnter).Model));
		bool flag3 = TruckerJobScript.CanUseVehicleForTrucking(vehicleTryingToEnter);
		if (!flag && !flag2 && !flag3)
		{
			return;
		}
		bool isLocked = false;
		if ((Entity)(object)vehicleTryingToEnter.Driver == (Entity)null || ((PoolObject)vehicleTryingToEnter.Driver).Handle == 0 || (Entity)(object)vehicleTryingToEnter.Driver == (Entity)(object)Game.PlayerPed)
		{
			if (flag)
			{
				isLocked = !cachedJobEnum.IsPolice() && !CuffedScript.IsCuffed;
			}
			else if (flag2)
			{
				isLocked = !cachedJobEnum.IsEMSOrFD();
			}
			else if (flag3)
			{
				isLocked = cachedJobEnum != JobsEnum.DeliveryDriver;
			}
		}
		SetLocked(vehicleTryingToEnter, isLocked);
		static void SetLocked(Vehicle vehicle, bool flag4, bool canEnter = true)
		{
			vehicle.NeedsToBeHotwired = flag4;
			vehicle.LockStatus = (VehicleLockStatus)((!canEnter) ? 10 : ((!flag4) ? 1 : 7));
		}
	}
}
