using System.Linq;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Gtacnr.Client.Anticheat;
using Gtacnr.Localization;
using Gtacnr.Model;
using MenuAPI;

namespace Gtacnr.Client.Jobs.PrivateMedic;

public class PrivateMedicServicesScript : Script
{
	[EventHandler("gtacnr:privateMedic:getHealed")]
	private async void OnGotHealed(int medicId)
	{
		Utils.ClearPedDamage(Game.PlayerPed);
		lock (AntiHealthLockScript.HealThreadLock)
		{
			AntiHealthLockScript.JustHealed();
			((Entity)Game.PlayerPed).HealthFloat = API.GetPedMaxHealth(API.PlayerPedId());
		}
		if (medicId <= 0)
		{
			return;
		}
		MenuItem menuItem = SellToPlayersScript.Menus["seller"].GetMenuItems().FirstOrDefault((MenuItem itm) => (itm.ItemData as Service).Type == "heal");
		if (menuItem != null)
		{
			menuItem.Label = LocalizationController.S(Entries.Main.NOT_AVAILABLE_SHORT);
			while (!menuItem.Enabled)
			{
				await BaseScript.Delay(0);
			}
			menuItem.Enabled = false;
		}
	}
}
