using System.Collections.Generic;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Gtacnr.Client.API;
using Gtacnr.Client.Businesses.Dealerships;
using Gtacnr.Client.Vehicles;
using Gtacnr.Localization;
using Gtacnr.Model;

namespace Gtacnr.Client.Jobs.Police;

public class UnmarkedVehiclesScript : Script
{
	private static HashSet<int> umkCarModels = new HashSet<int>();

	public static HashSet<int> UnmarkedCarModels => umkCarModels;

	protected override async void OnStarted()
	{
		while (!DealershipScript.WasDataLoaded)
		{
			await BaseScript.Delay(0);
		}
		foreach (PersonalVehicleModel value in DealershipScript.VehicleModelData.Values)
		{
			if (value.IsUnmarked)
			{
				umkCarModels.Add(API.GetHashKey(value.Id));
			}
		}
		VehicleEvents.EnteredVehicle += OnEnteredVehicle;
	}

	private async void OnEnteredVehicle(object sender, VehicleEventArgs e)
	{
		if (Gtacnr.Client.API.Jobs.CachedJobEnum.IsPolice() && UnmarkedCarModels.Contains(Model.op_Implicit(((Entity)Game.PlayerPed.CurrentVehicle).Model)))
		{
			Utils.DisplayHelpText(LocalizationController.S(Entries.Vehicles.INFO_UNMARKED_VEHICLE));
		}
	}
}
