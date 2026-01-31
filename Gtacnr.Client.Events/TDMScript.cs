using CitizenFX.Core;
using Gtacnr.Client.Anticheat;

namespace Gtacnr.Client.Events;

public class TDMScript : Script
{
	[EventHandler("gtacnr:events:tdm:restoreHealth")]
	private async void OnRestoreHealth()
	{
		lock (AntiHealthLockScript.HealThreadLock)
		{
			AntiHealthLockScript.JustHealed();
			((Entity)Game.PlayerPed).Health = 300;
		}
		lock (AntiHealthLockScript.ArmorThreadLock)
		{
			AntiHealthLockScript.JustUsedArmor();
			Game.PlayerPed.Armor = 200;
		}
		await BaseScript.Delay(2000);
		BaseScript.TriggerEvent("gtacnr:disableSpawnProtection", new object[0]);
	}
}
