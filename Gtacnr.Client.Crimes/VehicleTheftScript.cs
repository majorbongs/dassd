using System.Collections.Generic;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Gtacnr.Client.API;
using Gtacnr.Client.Communication;
using Gtacnr.Client.IMenu;
using Gtacnr.Client.Jobs.Police.Arrest;
using Gtacnr.Client.Vehicles;
using Gtacnr.Model;

namespace Gtacnr.Client.Crimes;

public class VehicleTheftScript : Script
{
	private List<Vehicle> stolenVehicles = new List<Vehicle>();

	public VehicleTheftScript()
	{
		VehicleEvents.EnteringVehicle += OnEnteringVehicle;
		VehicleEvents.EnteredVehicle += OnEnteringVehicle;
	}

	private async void OnEnteringVehicle(object sender, VehicleEventArgs e)
	{
		if (Gtacnr.Client.API.Jobs.CachedJobEnum.IsPublicService() || CuffedScript.IsCuffed || CuffedScript.IsBeingCuffedOrUncuffed || VehicleSlideshowScript.IsInSlideshow)
		{
			return;
		}
		Vehicle vehicle = Game.PlayerPed.CurrentVehicle;
		int jackingTargetPlayerId = 0;
		bool isJacking = e.Vehicle.Exists() && API.GetSeatPedIsTryingToEnter(((PoolObject)Game.PlayerPed).Handle) == -1 && (Entity)(object)e.Vehicle.Driver != (Entity)null && e.Vehicle.Driver.Exists() && (Entity)(object)e.Vehicle.Driver != (Entity)(object)Game.PlayerPed;
		if (isJacking)
		{
			vehicle = e.Vehicle;
			if (e.Vehicle.Driver.IsPlayer)
			{
				int num = API.NetworkGetPlayerIndexFromPed(((PoolObject)e.Vehicle.Driver).Handle);
				jackingTargetPlayerId = API.GetPlayerServerId(num);
				if (PartyScript.PartyMembers.Contains(jackingTargetPlayerId))
				{
					isJacking = false;
					jackingTargetPlayerId = 0;
				}
			}
		}
		bool isBreakingIn = e.Vehicle.Exists() && API.GetVehicleDoorLockStatus(((PoolObject)e.Vehicle).Handle) > 1;
		if (isBreakingIn)
		{
			vehicle = e.Vehicle;
		}
		if ((Entity)(object)vehicle == (Entity)null || stolenVehicles.Contains(vehicle) || ((Entity)(object)vehicle.Driver != (Entity)(object)Game.PlayerPed && (Entity)(object)vehicle == (Entity)(object)Game.PlayerPed.CurrentVehicle) || (Entity)(object)ActiveVehicleScript.ActiveVehicle == (Entity)(object)vehicle || (Entity)(object)VehiclesMenuScript.CurrentSummonVehicle == (Entity)(object)vehicle)
		{
			return;
		}
		bool isAlarmSounding = vehicle.Exists() && vehicle.IsAlarmSounding;
		if (isAlarmSounding)
		{
			isBreakingIn = true;
		}
		bool isRestricted = Gtacnr.Utils.IsVehicleModelAPoliceVehicle(((Entity)vehicle).Model.Hash) || Gtacnr.Utils.IsVehicleModelAnArmoredEmergencyVehicle(((Entity)vehicle).Model.Hash) || Gtacnr.Utils.IsVehicleModelAParamedicVehicle(((Entity)vehicle).Model.Hash) || Gtacnr.Utils.IsVehicleModelAFireDeptVehicle(((Entity)vehicle).Model.Hash) || Gtacnr.Utils.IsVehicleModelAMilitaryVehicle(((Entity)vehicle).Model.Hash);
		if (isJacking)
		{
			for (int i = 0; i < 5; i++)
			{
				await BaseScript.Delay(100);
				if (API.GetSeatPedIsTryingToEnter(((PoolObject)Game.PlayerPed).Handle) != -1)
				{
					return;
				}
			}
		}
		if (!isRestricted && !isBreakingIn && !isJacking)
		{
			return;
		}
		if (isRestricted || isJacking || isAlarmSounding)
		{
			ReportCrime();
			return;
		}
		foreach (Player player in ((BaseScript)this).Players)
		{
			PlayerState playerState = LatentPlayers.Get(player);
			if (playerState != null && playerState.JobEnum.IsPolice())
			{
				Vector3 position = ((Entity)player.Character).Position;
				if (((Vector3)(ref position)).DistanceToSquared(((Entity)Game.PlayerPed).Position) < 2500f)
				{
					ReportCrime();
					break;
				}
			}
		}
		void ReportCrime()
		{
			BaseScript.TriggerServerEvent("gtacnr:crimes:vehicleStolen", new object[5]
			{
				((Entity)vehicle).NetworkId,
				((Entity)vehicle).Model.Hash,
				isBreakingIn,
				isJacking,
				jackingTargetPlayerId
			});
			stolenVehicles.Add(vehicle);
		}
	}

	[EventHandler("gtacnr:respawned")]
	private void OnRespawned()
	{
		stolenVehicles.Clear();
	}

	[EventHandler("gtacnr:police:onArrested")]
	private void OnArrested()
	{
		stolenVehicles.Clear();
	}
}
