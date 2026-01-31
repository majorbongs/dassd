using CitizenFX.Core;
using CitizenFX.Core.Native;
using Gtacnr.Client.Sync;

namespace Gtacnr.Client.Events;

public class WinterBashScript : Script
{
	[EventHandler("gtacnr:winterbash:init")]
	private void InitializeWinterBash()
	{
		WeatherSyncScript.OverrideWeather = "XMAS";
		API.ClearOverrideWeather();
		API.ClearWeatherTypePersist();
		API.SetWeatherTypeNowPersist("XMAS");
		Utils.UpdateWeatherParticles("XMAS");
	}
}
