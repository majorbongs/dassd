using CitizenFX.Core;
using CitizenFX.Core.Native;

namespace Gtacnr.Client.Sync;

public class WeatherSyncScript : Script
{
	public static string? OverrideWeather;

	protected override void OnStarted()
	{
		BaseScript.TriggerServerEvent("gtacnr:syncWeather", new object[0]);
	}

	[EventHandler("gtacnr:syncWeather")]
	private async void OnSyncWeather(int weatherIdx, float windSpeed, float windDirection, bool immediately = false)
	{
		if (OverrideWeather != null)
		{
			API.SetWeatherTypeNowPersist(OverrideWeather);
			return;
		}
		API.SetWind(windSpeed);
		API.SetWindDirection(windDirection);
		API.ClearOverrideWeather();
		API.ClearWeatherTypePersist();
		WeatherInfo weatherInfo = WeatherInfo.All[weatherIdx];
		Print("Weather: " + weatherInfo.Description);
		if (immediately)
		{
			API.SetWeatherTypeNowPersist(weatherInfo.Id);
			World.Weather = weatherInfo.Weather;
		}
		else
		{
			World.TransitionToWeather(weatherInfo.Weather, 45f);
		}
	}
}
