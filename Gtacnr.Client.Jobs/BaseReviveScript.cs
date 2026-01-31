using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Gtacnr.Client.API;
using Gtacnr.Client.API.UI;
using Gtacnr.Client.Characters.Lifecycle;
using Gtacnr.Client.Jobs.Police.Arrest;
using Gtacnr.Localization;
using Gtacnr.Model;
using Gtacnr.Model.Enums;

namespace Gtacnr.Client.Jobs;

public abstract class BaseReviveScript : Script
{
	protected readonly int KEYBOARD_CONTROL_REVIVE = 252;

	protected readonly int GAMEPAD_CONTROL_REVIVE = 303;

	protected readonly int HOLD_TIME = 500;

	protected int closestDeadPlayerLocalId = -1;

	private bool instructionsShown;

	private bool manuallyCancelled;

	public static bool IsReviving { get; protected set; }

	protected abstract string StartReviveEvent { get; }

	protected abstract string EndReviveEvent { get; }

	protected abstract string CancelReviveEvent { get; }

	protected abstract int ReviveSeconds { get; }

	protected abstract bool IsJobAllowed(JobsEnum jobId);

	public BaseReviveScript()
	{
		Gtacnr.Client.API.Jobs.JobChangedEvent += OnJobChangedEvent;
	}

	private void OnJobChangedEvent(object sender, JobArgs e)
	{
		bool flag = IsJobAllowed(e.PreviousJobEnum);
		bool flag2 = IsJobAllowed(e.CurrentJobEnum);
		if (!flag && flag2)
		{
			base.Update += FindClosestDeadPlayerTick;
			base.Update += ControlsTick;
		}
		else if (flag && !flag2)
		{
			base.Update -= FindClosestDeadPlayerTick;
			base.Update -= ControlsTick;
			DisableInstructionalButtons();
		}
	}

	private async Coroutine FindClosestDeadPlayerTick()
	{
		await Script.Wait(500);
		if ((Entity)(object)Game.PlayerPed == (Entity)null)
		{
			return;
		}
		if (!IsJobAllowed(Gtacnr.Client.API.Jobs.CachedJobEnum) || Game.PlayerPed.IsInVehicle() || ((Entity)Game.PlayerPed).IsDead)
		{
			DisableInstructionalButtons();
			return;
		}
		Vector3 position = ((Entity)Game.PlayerPed).Position;
		float num = 4f;
		closestDeadPlayerLocalId = -1;
		foreach (Player player in ((BaseScript)this).Players)
		{
			if (!((Entity)(object)player.Character == (Entity)null) && player.Handle != Game.Player.Handle && ((Entity)player.Character).IsDead && !player.Character.IsInVehicle())
			{
				float num2 = ((Vector3)(ref position)).DistanceToSquared(((Entity)player.Character).Position);
				if (num2 < num)
				{
					num = num2;
					closestDeadPlayerLocalId = player.Handle;
				}
			}
		}
		if (closestDeadPlayerLocalId != -1 && !IsReviving)
		{
			EnableInstructionalButtons();
		}
		else
		{
			DisableInstructionalButtons();
		}
	}

	private async Coroutine ControlsTick()
	{
		if (closestDeadPlayerLocalId == -1 || IsReviving || CuffedScript.IsCuffed || !IsJobAllowed(Gtacnr.Client.API.Jobs.CachedJobEnum))
		{
			return;
		}
		bool pressed = false;
		if (Utils.IsUsingKeyboard())
		{
			pressed = API.IsControlJustPressed(2, KEYBOARD_CONTROL_REVIVE);
		}
		else if (API.IsControlJustPressed(2, GAMEPAD_CONTROL_REVIVE))
		{
			DateTime gamePadPressTimestamp = DateTime.UtcNow;
			while (API.IsControlPressed(2, GAMEPAD_CONTROL_REVIVE))
			{
				await Script.Yield();
				if (Gtacnr.Utils.CheckTimePassed(gamePadPressTimestamp, HOLD_TIME))
				{
					pressed = true;
					break;
				}
			}
		}
		if (pressed)
		{
			Player val = ((IEnumerable<Player>)((BaseScript)this).Players).FirstOrDefault((Player p) => p.Handle == closestDeadPlayerLocalId);
			if (val != (Player)null)
			{
				await RevivePlayer(val);
			}
		}
	}

	protected async Task RevivePlayer(Player target)
	{
		if ((Entity)(object)target.Character == (Entity)null || !((Entity)target.Character).IsDead || IsReviving)
		{
			return;
		}
		IsReviving = true;
		try
		{
			PlayerState targetInfo = LatentPlayers.Get(target);
			if (DeathScript.HasSpawnProtection)
			{
				BaseScript.TriggerEvent("gtacnr:disableSpawnProtection", new object[0]);
				BaseScript.TriggerServerEvent("gtacnr:spawnProtectionDisabled", new object[0]);
			}
			Vector3 val = ((Entity)target.Character).Position + ((Entity)target.Character).RightVector * 0.65f;
			Game.PlayerPed.Task.GoTo(val, true, 1500);
			await Script.Wait(1500);
			API.TaskTurnPedToFaceEntity(((PoolObject)Game.PlayerPed).Handle, ((PoolObject)target.Character).Handle, -1);
			await Script.Wait(500);
			Game.PlayerPed.Task.PlayAnimation("mini@cpr@char_a@cpr_str", "cpr_pumpchest", 8f, -1, (AnimationFlags)1);
			Vector3 reviveStartedPos = ((Entity)Game.PlayerPed).Position;
			int reviveStartedHealth = ((Entity)Game.PlayerPed).Health;
			if (await TriggerServerEventAsync<bool>(StartReviveEvent, new object[1] { target.ServerId }))
			{
				bool abort = false;
				for (int i = 0; i < ReviveSeconds * 4; i++)
				{
					await Script.Wait(250);
					if (AbortCondition(target, reviveStartedPos, reviveStartedHealth))
					{
						abort = true;
						BaseScript.TriggerServerEvent(CancelReviveEvent, new object[0]);
						if (manuallyCancelled)
						{
							Game.PlayerPed.Task.ClearAllImmediately();
							Utils.SendNotification(LocalizationController.S(Entries.Jobs.PARAMEDIC_REVIVAL_CANCELED));
						}
						else
						{
							Utils.SendNotification(LocalizationController.S(Entries.Jobs.PARAMEDIC_REVIVAL_FAIL));
						}
						break;
					}
				}
				bool flag = !abort;
				if (flag)
				{
					flag = await TriggerServerEventAsync<bool>(EndReviveEvent, new object[0]);
				}
				if (flag)
				{
					Utils.DisplayHelpText(LocalizationController.S(Entries.Jobs.PARAMEDIC_PLAYER_REVIVED, targetInfo.ColorNameAndId));
				}
			}
			Game.PlayerPed.Task.ClearAnimation("mini@cpr@char_a@cpr_str", "cpr_pumpchest");
		}
		catch (Exception exception)
		{
			Print(exception);
		}
		finally
		{
			IsReviving = false;
		}
	}

	protected virtual bool AbortCondition(Player target, Vector3 reviveStartedPos, int startingHealth)
	{
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		if (!((Entity)(object)target.Character == (Entity)null) && !((Entity)(object)Game.PlayerPed == (Entity)null) && ((Entity)Game.PlayerPed).Health >= startingHealth && !manuallyCancelled)
		{
			return ((Vector3)(ref reviveStartedPos)).DistanceToSquared(((Entity)Game.PlayerPed).Position) > 6.25f;
		}
		return true;
	}

	private void EnableInstructionalButtons()
	{
		if (!instructionsShown)
		{
			instructionsShown = true;
			if (Utils.IsUsingKeyboard())
			{
				Utils.AddInstructionalButton("revivePlayer", new InstructionalButton("Resuscitate", 2, KEYBOARD_CONTROL_REVIVE));
			}
			else
			{
				Utils.AddInstructionalButton("revivePlayer", new InstructionalButton("Resuscitate (hold)", 2, GAMEPAD_CONTROL_REVIVE));
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
