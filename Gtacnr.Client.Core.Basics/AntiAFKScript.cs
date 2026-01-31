using System;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Gtacnr.Client.Admin;
using Gtacnr.Client.Characters.Editor;
using Gtacnr.Client.Characters.Lifecycle;
using Gtacnr.Client.HUD;
using Gtacnr.Client.Premium;
using Gtacnr.Client.Tutorials;
using MenuAPI;
using NativeUI;

namespace Gtacnr.Client.Core.Basics;

public class AntiAFKScript : Script
{
	private readonly TimeSpan KICK_TIMER_START = TimeSpan.FromSeconds(120.0);

	private readonly TimeSpan KICK_TIMER = TimeSpan.FromSeconds(300.0);

	private DateTime lastActionT = DateTime.UtcNow;

	private bool isAFK;

	private TextTimerBar timerBar;

	private int lastMenuIndex;

	private void GoAFK()
	{
		if (!isAFK)
		{
			isAFK = true;
		}
	}

	private void CancelAFK()
	{
		lastActionT = DateTime.UtcNow;
		if (isAFK)
		{
			isAFK = false;
			if (timerBar != null)
			{
				TimerBarScript.RemoveTimerBar(timerBar);
				timerBar = null;
			}
		}
	}

	[Update]
	private async Coroutine CheckAFKTask()
	{
		await Script.Wait(100);
		if (!SpawnScript.HasSpawned)
		{
			lastActionT = DateTime.UtcNow;
			return;
		}
		if (Game.PlayerPed.IsWalking || Game.PlayerPed.IsRunning || Game.PlayerPed.IsSprinting || Game.PlayerPed.IsJumping || Game.PlayerPed.IsShooting || Game.PlayerPed.IsInMeleeCombat || Game.PlayerPed.IsSwimming || Game.PlayerPed.IsFalling || ((Entity)(object)Game.PlayerPed.CurrentVehicle != (Entity)null && (Entity)(object)Game.PlayerPed.CurrentVehicle.Driver == (Entity)(object)Game.PlayerPed && Game.PlayerPed.CurrentVehicle.Speed > 0.1f) || ((Entity)(object)Game.PlayerPed.CurrentVehicle != (Entity)null && (Entity)(object)Game.PlayerPed.CurrentVehicle.Driver != (Entity)null && (Entity)(object)Game.PlayerPed.CurrentVehicle.Driver != (Entity)(object)Game.PlayerPed) || CharacterCreationScript.IsInCreator || Utils.IsSwitchInProgress() || Utils.IsScreenFadingInProgress() || API.IsScreenFadedOut() || TutorialScript.IsInTutorial || (int)StaffLevelScript.StaffLevel > 0 || (int)MembershipScript.MembershipTier >= 2)
		{
			CancelAFK();
		}
		if (MenuController.IsAnyMenuOpen())
		{
			Menu currentMenu = MenuController.GetCurrentMenu();
			if (currentMenu.CurrentIndex != lastMenuIndex)
			{
				lastMenuIndex = currentMenu.CurrentIndex;
				CancelAFK();
			}
		}
		if (Gtacnr.Utils.CheckTimePassed(lastActionT, 10000.0))
		{
			GoAFK();
		}
		TimeSpan timeSpan = DateTime.UtcNow - lastActionT;
		TimeSpan cooldownTimeLeft = Gtacnr.Utils.GetCooldownTimeLeft(lastActionT, KICK_TIMER);
		if (timeSpan >= KICK_TIMER_START && timerBar == null)
		{
			timerBar = new TextTimerBar("AFK KICK", "");
			timerBar.TextColor = Colors.GTARed;
			TimerBarScript.AddTimerBar(timerBar);
			Game.PlaySound("HUD_FRONTEND_DEFAULT_SOUNDSET", "EXIT");
		}
		if (timerBar != null)
		{
			timerBar.Text = Gtacnr.Utils.SecondsToMinutesAndSeconds(cooldownTimeLeft.TotalSeconds.ToInt()) ?? "";
		}
		if (cooldownTimeLeft <= TimeSpan.Zero)
		{
			CancelAFK();
			BaseScript.TriggerServerEvent("gtacnr:afkKick", new object[0]);
		}
	}

	[EventHandler("gtacnr:chat:input")]
	private void OnChatInput()
	{
		CancelAFK();
	}

	[EventHandler("gtacnr:keys:event")]
	private void OnKeysEvent()
	{
		CancelAFK();
	}

	[EventHandler("gtacnr:keybinds:event")]
	private void OnKeybindsEvent()
	{
		CancelAFK();
	}
}
