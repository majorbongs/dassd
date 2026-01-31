using System;
using CitizenFX.Core;
using Gtacnr.Client.API;

namespace Gtacnr.Client.HUD;

public class XPDisplayScript : Script
{
	private DateTime lastDisplayTimestamp;

	public static bool XPBarHidden
	{
		set
		{
			BaseScript.TriggerEvent("Exp_XNL_SetHidden", new object[1] { value });
		}
	}

	public XPDisplayScript()
	{
		XPBarHidden = Preferences.XPBarHidden.Get();
		Users.XPChanged += OnXPChanged;
	}

	[EventHandler("gtacnr:spawned")]
	private async void OnSpawned()
	{
		int num = await Users.GetXP(force: true);
		BaseScript.TriggerEvent("XNL_NET:XNL_SetInitialXPLevels", new object[3] { num, true, false });
	}

	private void OnXPChanged(object sender, Users.XPEventArgs e)
	{
		int oldXP = e.OldXP;
		int newXP = e.NewXP;
		int amount = e.Amount;
		int bonus = e.Bonus;
		if (newXP > oldXP)
		{
			BaseScript.TriggerEvent("XNL_NET:AddPlayerXP", new object[1] { Math.Abs(amount) });
		}
		else if (newXP < oldXP)
		{
			BaseScript.TriggerEvent("XNL_NET:RemovePlayerXP", new object[1] { Math.Abs(amount) });
		}
		lastDisplayTimestamp = DateTime.UtcNow;
		BaseScript.TriggerEvent("gtacnr:hud:setXPChange", new object[2]
		{
			amount - bonus,
			bonus
		});
		BaseScript.TriggerEvent("gtacnr:hud:toggleXPChange", new object[1] { true });
		Hide();
		int levelByXP = Gtacnr.Utils.GetLevelByXP(oldXP);
		int levelByXP2 = Gtacnr.Utils.GetLevelByXP(newXP);
		if (levelByXP2 > levelByXP)
		{
			Game.PlaySound("RANK_UP", "HUD_AWARDS");
			Utils.DisplayHelpText($"~b~Congratulations! ~s~You are now level ~b~{levelByXP2}~s~.", playSound: false);
		}
		Print(string.Format("XP: {0}{1}{2}", (amount >= 0) ? "^2+" : "^1-", Math.Abs(amount), (levelByXP2 > levelByXP) ? $" ^0- Leveled up: ^3{levelByXP2}^0" : ""));
		static async void Hide()
		{
			await BaseScript.Delay(6000);
			BaseScript.TriggerEvent("gtacnr:hud:toggleXPChange", new object[1] { false });
		}
	}
}
