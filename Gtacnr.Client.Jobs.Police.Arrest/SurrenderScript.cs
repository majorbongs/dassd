using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Gtacnr.Client.Anticheat;
using Gtacnr.Client.API;
using Gtacnr.Client.API.UI;
using Gtacnr.Client.Characters.Lifecycle;

namespace Gtacnr.Client.Jobs.Police.Arrest;

public class SurrenderScript : Script
{
	private readonly int KEYBOARD_CONTROL = 252;

	private readonly int GAMEPAD_CONTROL = 303;

	private readonly int HOLD_TIME = 500;

	private readonly int COOLDOWN = 2000;

	public static bool IsSurrendered;

	public static bool IsHoldingHandsUp;

	private bool instructionalButtonShown;

	private bool wasSurrenderedLastFrame;

	private bool surrenderInProgress;

	private DateTime surrenderTimestamp;

	private float health;

	private DateTime lastDamageReceivedTimestamp;

	public SurrenderScript()
	{
		DeathScript.Respawning += OnRespawning;
	}

	[Update]
	private async Coroutine ControlTick()
	{
		if (Gtacnr.Client.API.Jobs.CachedJobEnum.IsPublicService() || Gtacnr.Client.API.Jobs.CachedJobEnum.IsAbleToRevive())
		{
			return;
		}
		float num = health;
		health = ((Entity)Game.PlayerPed).Health;
		if (health < num)
		{
			lastDamageReceivedTimestamp = DateTime.UtcNow;
		}
		if (IsSurrendered || IsHoldingHandsUp)
		{
			API.DisableControlAction(1, 37, true);
			API.DisableControlAction(1, 12, true);
			API.DisableControlAction(1, 13, true);
			API.DisableControlAction(1, 14, true);
			API.DisableControlAction(1, 15, true);
			API.DisableControlAction(1, 16, true);
			API.DisableControlAction(1, 17, true);
			API.SetCurrentPedWeapon(API.PlayerPedId(), 2725352035u, true);
			if (!surrenderInProgress && IsSurrendered)
			{
				AnimationFlags val = (AnimationFlags)2;
				if (!API.IsEntityPlayingAnim(((PoolObject)Game.PlayerPed).Handle, "random@arrests", "kneeling_arrest_idle", (int)val))
				{
					await Game.PlayerPed.Task.PlayAnimation("random@arrests", "kneeling_arrest_idle", 3f, -3f, -1, val, 0f);
				}
			}
		}
		if (!IsSurrendered)
		{
			if (wasSurrenderedLastFrame)
			{
				API.EnableControlAction(1, 37, true);
				API.EnableControlAction(1, 12, true);
				API.EnableControlAction(1, 13, true);
				API.EnableControlAction(1, 14, true);
				API.EnableControlAction(1, 15, true);
				API.EnableControlAction(1, 16, true);
				API.EnableControlAction(1, 17, true);
			}
			bool isKb = Utils.IsUsingKeyboard();
			if (API.IsControlJustPressed(2, isKb ? KEYBOARD_CONTROL : GAMEPAD_CONTROL))
			{
				DateTime pressT = DateTime.UtcNow;
				while (API.IsControlPressed(2, isKb ? KEYBOARD_CONTROL : GAMEPAD_CONTROL))
				{
					await Script.Yield();
					if (Gtacnr.Utils.CheckTimePassed(pressT, HOLD_TIME))
					{
						PerformAction();
						break;
					}
				}
				if (isKb && !IsSurrendered && !CuffedScript.IsBeingCuffedOrUncuffed && !CuffedScript.IsCuffed && ((Entity)Game.PlayerPed).IsAlive && !Gtacnr.Utils.CheckTimePassed(pressT, HOLD_TIME))
				{
					if (API.IsEntityPlayingAnim(API.PlayerPedId(), "missminuteman_1ig_2", "handsup_enter", 3))
					{
						Game.PlayerPed.Task.ClearAnimation("missminuteman_1ig_2", "handsup_enter");
						IsHoldingHandsUp = false;
					}
					else if (!Game.PlayerPed.IsInVehicle())
					{
						API.RequestAnimDict("missminuteman_1ig_2");
						while (!API.HasAnimDictLoaded("missminuteman_1ig_2"))
						{
							await Script.Yield();
						}
						API.TaskPlayAnim(API.PlayerPedId(), "missminuteman_1ig_2", "handsup_enter", 4f, 4f, -1, 50, 0f, false, false, false);
						IsHoldingHandsUp = true;
						await Script.Wait(100);
					}
				}
			}
		}
		if (IsHoldingHandsUp && !API.IsEntityPlayingAnim(API.PlayerPedId(), "missminuteman_1ig_2", "handsup_enter", 3))
		{
			IsHoldingHandsUp = false;
		}
		wasSurrenderedLastFrame = IsSurrendered;
		void PerformAction()
		{
			if (Gtacnr.Utils.CheckTimePassed(surrenderTimestamp, COOLDOWN))
			{
				surrenderTimestamp = DateTime.UtcNow;
				DisableInstructionalButtons();
				Surrender();
			}
		}
	}

	[EventHandler("gtacnr:police:onAskedToSurrender")]
	private async void OnAskedToSurrender(int copId, int action)
	{
		string arg = await Authentication.GetAccountName(copId);
		_ = ((PoolObject)Game.PlayerPed).Handle;
		List<string> actions = new List<string> { "pull over", "freeze", "drop your weapons" };
		if (action < actions.Count && !CuffedScript.IsInCustody)
		{
			Utils.DisplayHelpText($"Officer ~b~{arg} ({copId}) ~s~asked you to ~r~{actions[action]}!");
			await Utils.ShakeGamepad();
			if (action.In(0, 1) && Crime.CachedWantedLevel < 2)
			{
				Utils.SendNotification("<C>~y~Tip:</C> ~s~You should ~b~" + actions[action] + " ~s~to avoid being charged with a ~o~felony~s~.");
			}
			if (action != 0)
			{
				EnableInstructionalButtons();
				await BaseScript.Delay(5000);
				DisableInstructionalButtons();
			}
		}
	}

	[EventHandler("gtacnr:police:onSurrender")]
	private async void OnSurrender(int playerId, int wantedLevel)
	{
		if (!(Gtacnr.Client.API.Jobs.CachedJob != "police"))
		{
			string col = "~o~";
			if (wantedLevel == 5)
			{
				col = "~r~";
			}
			Utils.DisplayHelpText($"{col}{await Authentication.GetAccountName(playerId)} ({playerId}) ~s~surrenders! ~r~Do not shoot.");
		}
	}

	[EventHandler("gtacnr:police:stopSurrendering")]
	private async void OnStopSurrendering()
	{
		IsSurrendered = false;
		Utils.DisplayHelpText("The ~b~cops ~s~are no longer around!");
		await Utils.ShakeGamepad(1000, 500);
		string dict1 = "random@arrests";
		API.RequestAnimDict(dict1);
		while (!API.HasAnimDictLoaded(dict1))
		{
			await BaseScript.Delay(5);
		}
		AnimationFlags val = (AnimationFlags)2;
		await Game.PlayerPed.Task.PlayAnimation(dict1, "kneeling_arrest_get_up", 3f, -3f, -1, val, 0f);
		await BaseScript.Delay(3000);
		Game.PlayerPed.Task.ClearAll();
	}

	private void OnRespawning(object sender, EventArgs e)
	{
		IsSurrendered = false;
	}

	[EventHandler("gtacnr:emsReviveCompleted")]
	private void OnRevived(bool revivedByPlayer, bool revivedByEMS)
	{
		IsSurrendered = false;
	}

	[EventHandler("gtacnr:police:cancelGetCuffed")]
	private void OnCancelGetCuffed(int officerServerId, bool resisted)
	{
		IsSurrendered = false;
	}

	private async void Surrender()
	{
		if (IsSurrendered || CuffedScript.IsInCustody || CuffedScript.IsBeingCuffedOrUncuffed || Crime.CachedWantedLevel < 2 || Gtacnr.Client.API.Jobs.CachedJobEnum.IsPublicService() || Game.PlayerPed.IsInVehicle() || ((Entity)Game.PlayerPed).IsDead || Game.PlayerPed.IsAiming)
		{
			return;
		}
		if (!Gtacnr.Utils.CheckTimePassed(lastDamageReceivedTimestamp, 1000.0))
		{
			Utils.PlayErrorSound();
		}
		else if (await TriggerServerEventAsync<int>("gtacnr:police:surrender", new object[0]) == 1)
		{
			surrenderInProgress = true;
			IsSurrendered = true;
			Utils.DisplayHelpText("You surrendered to the ~b~police~s~!");
			await Utils.ShakeGamepad();
			uint num = 0u;
			API.GetCurrentPedWeapon(API.PlayerPedId(), ref num, true);
			if (num != 0 && num != 2725352035u)
			{
				int weaponObjectFromPed = API.GetWeaponObjectFromPed(API.PlayerPedId(), true);
				API.NetworkRegisterEntityAsNetworked(weaponObjectFromPed);
				await AntiEntitySpawnScript.RegisterEntity(Entity.FromHandle(weaponObjectFromPed));
				await BaseScript.Delay(500);
			}
			if ((int)Game.PlayerPed.Weapons.Current.Hash != -1569615261)
			{
				Game.PlayerPed.Weapons.Select((WeaponHash)(-1569615261), true);
			}
			await KneelDownAnim();
			surrenderInProgress = false;
		}
	}

	private async Task KneelDownAnim()
	{
		string dict = "random@arrests";
		API.RequestAnimDict(dict);
		while (!API.HasAnimDictLoaded(dict))
		{
			await BaseScript.Delay(5);
		}
		AnimationFlags flags1 = (AnimationFlags)2;
		await Game.PlayerPed.Task.PlayAnimation(dict, "idle_2_hands_up", 3f, -3f, -1, flags1, 0f);
		await BaseScript.Delay(4000);
		await Game.PlayerPed.Task.PlayAnimation(dict, "kneeling_arrest_idle", 3f, -3f, -1, flags1, 0f);
		await BaseScript.Delay(3000);
	}

	private void EnableInstructionalButtons()
	{
		if (!instructionalButtonShown)
		{
			instructionalButtonShown = true;
			if (Utils.IsUsingKeyboard())
			{
				Utils.AddInstructionalButton("surrenderAction", new InstructionalButton("Surrender (hold)", 2, KEYBOARD_CONTROL));
			}
			else
			{
				Utils.AddInstructionalButton("surrenderAction", new InstructionalButton("Surrender (hold)", 2, GAMEPAD_CONTROL));
			}
		}
	}

	private void DisableInstructionalButtons()
	{
		if (instructionalButtonShown)
		{
			instructionalButtonShown = false;
			Utils.RemoveInstructionalButton("surrenderAction");
		}
	}
}
