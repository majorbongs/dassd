using CitizenFX.Core;
using CitizenFX.Core.Native;
using Gtacnr.Client.API;
using Gtacnr.Client.Sync;
using Gtacnr.Client.Vehicles;
using Gtacnr.Model;

namespace Gtacnr.Client.Events.Holidays.Halloween;

public class HalloweenScript : Script
{
	public static bool IsHalloween;

	private bool isDrivingVehicle;

	protected override async void OnStarted()
	{
		IsHalloween = await TriggerServerEventAsync<bool>("gtacnr:halloween:isHalloween", new object[0]);
		if (IsHalloween)
		{
			WeatherSyncScript.OverrideWeather = "FOGGY";
			TimeSyncScript.OverrideTime = new GameTime(23, 0);
			API.ClearOverrideWeather();
			API.ClearWeatherTypePersist();
			API.SetWeatherTypeNowPersist("FOGGY");
			BaseScript.TriggerEvent("gtacnr:halloween:initialize", new object[0]);
			VehicleEvents.EnteredVehicle += OnEnteredVehicle;
		}
	}

	private void OnEnteredVehicle(object sender, VehicleEventArgs e)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Invalid comparison between Unknown and I4
		if ((int)e.Seat == -1)
		{
			Vehicle vehicle = e.Vehicle;
			VehicleState vehicleState = LatentVehicleStateScript.Get(((Entity)vehicle).NetworkId);
			if (!Gtacnr.Utils.IsVehicleModelAPoliceVehicle(Model.op_Implicit(((Entity)vehicle).Model)) && string.IsNullOrEmpty(vehicleState?.PersonalVehicleId))
			{
				vehicle.Mods.PrimaryColor = (VehicleColor)41;
				vehicle.Mods.SecondaryColor = (VehicleColor)12;
			}
		}
	}

	[EventHandler("gtacnr:spawned")]
	private async void OnSpawned()
	{
		if (IsHalloween)
		{
			Utils.DisplaySubtitle("~h~~HUD_COLOUR_MENU_GREY_DARK~Happy ~o~Halloween!", 15000);
		}
	}
}
