using System;
using System.Collections.Generic;
using System.Linq;
using CitizenFX.Core;
using Gtacnr.Client.Anticheat;
using Gtacnr.Client.API;
using Gtacnr.Client.API.UI;
using Gtacnr.Localization;
using Gtacnr.Model;

namespace Gtacnr.Client.Jobs.Paramedic;

public class AmbulanceScript : Script
{
	private readonly Control keyboardControl = (Control)246;

	private readonly Control gamepadControl = (Control)303;

	private bool isParamedic;

	private bool instructionsShown;

	private List<int> playersToHeal = new List<int>();

	private Dictionary<int, DateTime> healTime = new Dictionary<int, DateTime>();

	[Update]
	private async Coroutine AmbulanceTick()
	{
		await Script.Wait(1000);
		isParamedic = Gtacnr.Client.API.Jobs.CachedJob == "paramedic";
		if (!isParamedic)
		{
			return;
		}
		playersToHeal.Clear();
		if ((Entity)(object)Game.PlayerPed == (Entity)null || (Entity)(object)Game.PlayerPed.CurrentVehicle == (Entity)null || ((Entity)Game.PlayerPed).IsDead)
		{
			return;
		}
		Vehicle currentVehicle = Game.PlayerPed.CurrentVehicle;
		if (!Gtacnr.Utils.IsVehicleModelAParamedicVehicle(Model.op_Implicit(((Entity)currentVehicle).Model)))
		{
			return;
		}
		foreach (Player item in ((IEnumerable<Player>)((BaseScript)this).Players).Where((Player p) => p.Handle != ((PoolObject)Game.PlayerPed).Handle && ((Entity)p.Character).Health < ((Entity)p.Character).MaxHealth))
		{
			if ((!healTime.ContainsKey(item.ServerId) || Gtacnr.Utils.CheckTimePassed(healTime[item.ServerId], TimeSpan.FromSeconds(60.0))) && !LatentPlayers.Get(item).AdminDuty && !((Entity)item.Character).IsDead && !item.Character.IsInVehicle() && !item.Character.IsInCombat && !item.Character.IsInMeleeCombat)
			{
				Vector3 position = ((Entity)item.Character).Position;
				if (!(((Vector3)(ref position)).DistanceToSquared(((Entity)currentVehicle).Position) > 64f))
				{
					playersToHeal.Add(item.ServerId);
				}
			}
		}
	}

	[Update]
	private async Coroutine ControlsTick()
	{
		if (playersToHeal.Count == 0)
		{
			DisableInstructionalButtons();
			return;
		}
		EnableInstructionalButtons();
		bool flag = Game.IsControlJustPressed(2, keyboardControl) && Utils.IsUsingKeyboard();
		if (!flag)
		{
			flag = await Utils.IsControlHeld(2, gamepadControl) && !Utils.IsUsingKeyboard();
		}
		if (!flag)
		{
			return;
		}
		foreach (int item in playersToHeal)
		{
			healTime[item] = DateTime.UtcNow;
		}
		Heal();
		async void Heal()
		{
			DisableInstructionalButtons();
			List<int> playersToHealCopy = playersToHeal.ToList();
			bool flag2 = ((IEnumerable<Player>)((BaseScript)this).Players).Any(delegate(Player p)
			{
				//IL_000c: Unknown result type (might be due to invalid IL or missing references)
				//IL_0011: Unknown result type (might be due to invalid IL or missing references)
				//IL_0019: Unknown result type (might be due to invalid IL or missing references)
				PlayerState? playerState3 = LatentPlayers.Get(p);
				Vector3 position = ((Entity)p.Character).Position;
				float num = ((Vector3)(ref position)).DistanceToSquared(((Entity)Game.PlayerPed).Position);
				return playerState3.JobEnum.IsPolice() && num <= 2500f;
			});
			bool flag3 = false;
			foreach (int playerId in playersToHeal)
			{
				Player obj = ((IEnumerable<Player>)((BaseScript)this).Players).FirstOrDefault((Player p) => p.ServerId == playerId);
				PlayerState playerState = LatentPlayers.Get(playerId);
				bool flag4 = false;
				if (obj.State["gtacnr:lastDamageT"] is long ticks && !Gtacnr.Utils.CheckTimePassed(new DateTime(ticks), 10000.0))
				{
					flag4 = true;
				}
				bool isCuffed = playerState.IsCuffed;
				if (playerState.WantedLevel > 1 && !isCuffed && flag4 && flag2)
				{
					playersToHealCopy.Remove(playerId);
					if (!flag3)
					{
						flag3 = true;
						Utils.DisplayHelpText("You can't heal ~o~criminals ~s~when they're involved in a ~r~shooting ~s~with the ~b~police~s~, as it is considered ~r~cross-teaming~s~.");
					}
				}
			}
			playersToHeal.Clear();
			switch (await TriggerServerEventAsync<int>("gtacnr:ems:healPlayers", new object[1] { playersToHealCopy.Json() }))
			{
			case 1:
				if (playersToHealCopy.Count == 1)
				{
					PlayerState playerState2 = LatentPlayers.Get(playersToHealCopy.First());
					Utils.DisplayHelpText("You ~p~healed " + playerState2.ColorNameAndId + ".");
				}
				else
				{
					Utils.DisplayHelpText($"You ~p~healed ~s~{playersToHealCopy.Count} players.");
				}
				break;
			default:
				Utils.DisplayHelpText(LocalizationController.S(Entries.Main.UNEXPECTED_ERROR_CODE, "0xA0"));
				break;
			case 2:
				break;
			}
		}
	}

	[EventHandler("gtacnr:ems:getHealed")]
	private async void GetHealed(int paramedicId)
	{
		if (((Entity)Game.PlayerPed).Health != 0)
		{
			PlayerState playerState = LatentPlayers.Get(paramedicId);
			Utils.DisplayHelpText("Paramedic " + playerState.ColorNameAndId + " has restored your health.");
			lock (AntiHealthLockScript.HealThreadLock)
			{
				AntiHealthLockScript.JustHealed();
				((Entity)Game.PlayerPed).Health = ((Entity)Game.PlayerPed).MaxHealth;
			}
			Utils.ClearPedDamage(Game.PlayerPed);
		}
	}

	private void EnableInstructionalButtons()
	{
		//IL_003f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		if (!instructionsShown)
		{
			instructionsShown = true;
			if (Utils.IsUsingKeyboard())
			{
				Utils.AddInstructionalButton("revivePlayer", new InstructionalButton("Heal", 2, keyboardControl));
			}
			else
			{
				Utils.AddInstructionalButton("revivePlayer", new InstructionalButton("Heal (hold)", 2, gamepadControl));
			}
		}
	}

	private void DisableInstructionalButtons()
	{
		if (instructionsShown)
		{
			instructionsShown = false;
			Utils.RemoveInstructionalButton("revivePlayer");
		}
	}
}
