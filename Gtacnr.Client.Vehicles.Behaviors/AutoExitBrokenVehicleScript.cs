using System;
using System.Collections.Generic;
using System.Linq;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Gtacnr.Localization;

namespace Gtacnr.Client.Vehicles.Behaviors;

public class AutoExitBrokenVehicleScript : Script
{
	private static readonly int[] affectedVehicleHashes = ((IEnumerable<string>)new string[1] { "pumpkinator" }).Select((Func<string, int>)API.GetHashKey).ToArray();

	[Update]
	private async Coroutine CheckVehicle()
	{
		await Script.Wait(5000);
		Ped playerPed = Game.PlayerPed;
		Vehicle val = ((playerPed != null) ? playerPed.CurrentVehicle : null);
		if (!((Entity)(object)val == (Entity)null) && (!API.IsVehicleDriveable(((PoolObject)val).Handle, false) || !(val.EngineHealth > 100f)) && affectedVehicleHashes.Contains(Model.op_Implicit(((Entity)val).Model)))
		{
			Game.PlayerPed.Task.LeaveVehicle((LeaveVehicleFlags)0);
			val.LockStatus = (VehicleLockStatus)10;
			Utils.DisplayHelpText(LocalizationController.S(Entries.Main.VEHICLE_CANNOT_BE_USED_BROKEN));
		}
	}
}
