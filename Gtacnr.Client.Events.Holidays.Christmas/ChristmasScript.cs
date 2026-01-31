using CitizenFX.Core;
using CitizenFX.Core.Native;
using Gtacnr.Client.Sync;

namespace Gtacnr.Client.Events.Holidays.Christmas;

public class ChristmasScript : Script
{
	public static bool IsChristmas;

	protected override async void OnStarted()
	{
		IsChristmas = await TriggerServerEventAsync<bool>("gtacnr:christmas:isChristmas", new object[0]);
		if (IsChristmas)
		{
			WeatherSyncScript.OverrideWeather = "XMAS";
			API.ClearOverrideWeather();
			API.ClearWeatherTypePersist();
			API.SetWeatherTypeNowPersist("XMAS");
			Utils.UpdateWeatherParticles("XMAS");
			BaseScript.TriggerEvent("gtacnr:christmas:initialize", new object[0]);
		}
	}

	[EventHandler("gtacnr:spawned")]
	private async void OnSpawned()
	{
		if (IsChristmas)
		{
			Utils.DisplaySubtitle("~h~~g~Merry ~r~Christmas ~s~and ~g~Happy ~r~New Year~s~!", 15000);
			await BaseScript.Delay(7500);
			Utils.DisplayHelpText("Press ~INPUT_DETONATE~ to pick up ~b~snowballs~s~.");
		}
	}
}
