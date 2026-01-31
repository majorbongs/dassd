using System;
using System.Collections.Generic;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Gtacnr.Client.API;
using Gtacnr.Client.API.UI;
using Gtacnr.Model;
using Gtacnr.Model.Enums;

namespace Gtacnr.Client.Jobs.Police.Arrest;

public class StopScript : Script
{
	private readonly int KEYBOARD_CONTROL = 252;

	private readonly int GAMEPAD_CONTROL = 303;

	private readonly int HOLD_TIME = 500;

	private readonly int COOLDOWN = 10000;

	private int targetPlayerId;

	private bool canStopPlayer;

	private bool canPullOver;

	private bool canAskToFreeze;

	private bool canAskToDropWeapons;

	private bool instructionalButtonShown;

	private DateTime stopTimestamp = DateTime.MinValue;

	private int lastWarnedPlayerId;

	private int numPullOverWarnings;

	[Update]
	private async Coroutine ControlsTick()
	{
		if (Utils.IsUsingKeyboard())
		{
			if (API.IsControlJustPressed(2, KEYBOARD_CONTROL))
			{
				PerformAction();
			}
		}
		else
		{
			if (!API.IsControlJustPressed(2, GAMEPAD_CONTROL))
			{
				return;
			}
			DateTime pressT = DateTime.UtcNow;
			while (API.IsControlPressed(2, GAMEPAD_CONTROL))
			{
				await Script.Yield();
				if (Gtacnr.Utils.CheckTimePassed(pressT, HOLD_TIME))
				{
					PerformAction();
					break;
				}
			}
		}
		void PerformAction()
		{
			if (Gtacnr.Utils.CheckTimePassed(stopTimestamp, COOLDOWN))
			{
				stopTimestamp = DateTime.UtcNow;
				DisableInstructionalButtons();
				AskToSurrender();
			}
		}
	}

	[Update]
	private async Coroutine TargetTick()
	{
		await Script.Wait(250);
		targetPlayerId = 0;
		canStopPlayer = false;
		canAskToFreeze = false;
		canPullOver = false;
		canAskToDropWeapons = false;
		if (!Gtacnr.Client.API.Jobs.CachedJobEnum.IsPolice() || ((Entity)Game.PlayerPed).IsDead)
		{
			DisableInstructionalButtons();
			return;
		}
		Vector3 position = ((Entity)Game.PlayerPed).Position;
		float num = 3600f;
		foreach (Player player in ((BaseScript)this).Players)
		{
			if (player.Handle == Game.Player.Handle)
			{
				continue;
			}
			PlayerState playerState = LatentPlayers.Get(player.ServerId);
			if (playerState != null && playerState.CanBeStopped && !API.IsPlayerDead(player.Handle))
			{
				_ = ((PoolObject)player.Character).Handle;
				Vector3 position2 = ((Entity)player.Character).Position;
				float num2 = ((Vector3)(ref position)).DistanceToSquared(position2);
				if (num2 < num)
				{
					num = num2;
					targetPlayerId = player.Handle;
				}
			}
		}
		if (targetPlayerId != 0)
		{
			int playerPed = API.GetPlayerPed(targetPlayerId);
			Vector3 entityCoords = API.GetEntityCoords(playerPed, true);
			float num3 = ((Vector3)(ref position)).DistanceToSquared(entityCoords);
			int vehiclePedIsIn = API.GetVehiclePedIsIn(playerPed, false);
			bool flag = ((PoolObject)new Vehicle(vehiclePedIsIn).Driver).Handle == playerPed;
			canPullOver = vehiclePedIsIn != 0 && API.GetEntitySpeed(vehiclePedIsIn) * 2.23f > 10f && flag;
			canAskToDropWeapons = vehiclePedIsIn == 0 && API.IsPedArmed(playerPed, 7);
			canAskToFreeze = vehiclePedIsIn == 0 && !API.IsPedStill(playerPed) && !canAskToDropWeapons;
			float num4 = 30f.Square();
			if (canPullOver || Game.PlayerPed.IsInHeli)
			{
				num4 = 60f.Square();
			}
			canStopPlayer = num3 < num4;
		}
		RefreshInstructionalButtons();
	}

	private void AskToSurrender()
	{
		if (!canStopPlayer || targetPlayerId == 0)
		{
			return;
		}
		bool flag = Game.PlayerPed.IsInVehicle();
		List<string> list = new List<string> { "pull over", "freeze", "drop their weapons" };
		int num = -1;
		string text = null;
		if (canPullOver)
		{
			num = 0;
			if (flag)
			{
				uint entityModel = (uint)API.GetEntityModel(API.GetVehiclePedIsIn(API.GetPlayerPed(targetPlayerId), false));
				string text2 = (API.IsThisModelABoat(entityModel) ? "BOAT" : (API.IsThisModelACar(entityModel) ? "CAR" : "VEHICLE"));
				if (lastWarnedPlayerId != targetPlayerId)
				{
					numPullOverWarnings = 0;
				}
				numPullOverWarnings++;
				lastWarnedPlayerId = targetPlayerId;
				text = ((numPullOverWarnings < 3) ? ("STOP_VEHICLE_" + text2 + "_MEGAPHONE") : ("STOP_VEHICLE_" + text2 + "_WARNING_MEGAPHONE"));
			}
			else
			{
				text = "FOOT_CHASE";
			}
		}
		else if (canAskToDropWeapons)
		{
			num = 2;
			text = "DRAW_GUN";
		}
		else if (canAskToFreeze)
		{
			num = 1;
			text = ((!flag) ? "FOOT_CHASE" : "STOP_ON_FOOT_MEGAPHONE");
		}
		if (num != -1)
		{
			int sex = ((API.GetEntityModel(API.PlayerPedId()) == API.GetHashKey("mp_f_freemode_01")) ? 1 : 0);
			int index = Preferences.PoliceVoiceIdx.Get();
			string voice = PoliceVoices.GetVoice((Sex)sex, index);
			int playerServerId = API.GetPlayerServerId(targetPlayerId);
			BaseScript.TriggerServerEvent("gtacnr:police:askToSurrender", new object[4] { playerServerId, num, text, voice });
			PlayerState playerState = LatentPlayers.Get(playerServerId);
			Utils.DisplayHelpText("You asked " + playerState.ColorNameAndId + " to ~b~" + list[num] + "!");
		}
	}

	private void RefreshInstructionalButtons()
	{
		if (!Gtacnr.Utils.CheckTimePassed(stopTimestamp, COOLDOWN))
		{
			DisableInstructionalButtons();
			return;
		}
		string text = "";
		if (canStopPlayer)
		{
			if (canPullOver)
			{
				text = "Pull over";
			}
			else if (canAskToFreeze)
			{
				text = "Ask to freeze";
			}
			else if (canAskToDropWeapons)
			{
				text = "Ask to drop weapons";
			}
		}
		if (text == "")
		{
			DisableInstructionalButtons();
		}
		else
		{
			EnableInstructionalButtons(text);
		}
	}

	private void EnableInstructionalButtons(string action)
	{
		if (!instructionalButtonShown)
		{
			instructionalButtonShown = true;
			if (Utils.IsUsingKeyboard())
			{
				Utils.AddInstructionalButton("surrenderAction", new InstructionalButton(action ?? "", 2, KEYBOARD_CONTROL));
			}
			else
			{
				Utils.AddInstructionalButton("surrenderAction", new InstructionalButton(action + " (hold)", 2, GAMEPAD_CONTROL));
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
