using System;
using System.Collections.Generic;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Gtacnr.Client.API;
using Gtacnr.Client.Characters.Lifecycle;
using Gtacnr.Localization;
using Gtacnr.Model;
using Gtacnr.Model.Enums;
using MenuAPI;

namespace Gtacnr.Client.Jobs.Hitman;

public class HitmanScript : Script
{
	private const float RADIUS = 350f;

	public static readonly TimeSpan TARGET_POSITION_UPDATE_DELAY = TimeSpan.FromMinutes(1.0);

	private static Blip blip = null;

	private static HitmanScript script;

	public static int CurrentTarget { get; private set; } = 0;

	public HitmanScript()
	{
		script = this;
		Gtacnr.Client.API.Jobs.JobChangedEvent += OnJobChangedEvent;
		DeathEventScript.PlayerDeath += OnPlayerDeath;
	}

	private void OnPlayerDeath(object sender, DeathEventArgs e)
	{
		if (Gtacnr.Client.API.Jobs.CachedJobEnum == JobsEnum.Hitman && e.KillerId != Game.Player.ServerId && e.VictimId == CurrentTarget && e.HitmanContractReward != 0)
		{
			PlayerState playerState = LatentPlayers.Get(CurrentTarget);
			PlayerState playerState2 = LatentPlayers.Get(e.KillerId);
			Utils.DisplayHelpText("The hit contract on " + playerState.ColorNameAndId + " has been ~r~completed ~s~by hitman " + playerState2.ColorNameAndId + ".");
		}
	}

	protected override void OnStarted()
	{
		Chat.AddSuggestion("/settarget", "Sets your target as a hitman.", new ChatParamSuggestion("target", "The id of the player you want to target as a hitman.", isOptional: true));
	}

	public static async void SetTarget(int target)
	{
		if (!HitmanDispatch.PlayerContracts.ContainsKey(target))
		{
			Utils.PlayErrorSound();
			return;
		}
		Cleanup();
		CurrentTarget = target;
		MenuController.CloseAllMenus();
		PlayerState playerState = LatentPlayers.Get(target);
		Utils.DisplayHelpText(LocalizationController.S(Entries.Jobs.HITMAN_TARGET_SELECTED, playerState.ColorNameAndId));
		await BaseScript.Delay(6000);
		Utils.DisplayHelpText(LocalizationController.S(Entries.Jobs.HITMAN_TARGET_SELECTED_HINT));
	}

	public static async void ShowNearbyTargetPosition()
	{
		if (CurrentTarget == 0)
		{
			return;
		}
		Vector3 val = await script.TriggerServerEventAsync<Vector3>("gtacnr:hitman:trackTarget", new object[1] { CurrentTarget });
		if (val == default(Vector3))
		{
			Utils.DisplayError(ResponseCode.GenericError, "Unable to fetch target's coords.", "ShowNearbyTargetPosition");
			return;
		}
		val.X = (float)Gtacnr.Utils.GetRandomDouble(val.X - 175f, val.X + 175f);
		val.Y = (float)Gtacnr.Utils.GetRandomDouble(val.Y - 175f, val.Y + 175f);
		val.Z = (float)Gtacnr.Utils.GetRandomDouble(val.Z - 175f, val.Z + 175f);
		if (blip == (Blip)null)
		{
			blip = World.CreateBlip(val, 350f);
			blip.IsShortRange = false;
			blip.Sprite = (BlipSprite)(-1);
			blip.Color = (BlipColor)44;
			blip.Alpha = 64;
			blip.Name = "Target Area";
			API.SetBlipDisplay(((PoolObject)blip).Handle, 6);
		}
		else
		{
			blip.Position = val;
		}
		string locationName = Utils.GetLocationName(val);
		PlayerState playerState = LatentPlayers.Get(CurrentTarget);
		Utils.DisplayHelpText(LocalizationController.S(Entries.Jobs.HITMAN_PAYPHONE_USED_HINT, playerState.ColorNameAndId, "~o~" + locationName + "~s~"));
	}

	private static void Cleanup()
	{
		if (CurrentTarget != 0)
		{
			CurrentTarget = 0;
			Blip obj = blip;
			if (obj != null)
			{
				((PoolObject)obj).Delete();
			}
			blip = null;
		}
	}

	[EventHandler("gtacnr:hitman:hitContractExpired")]
	private void OnHitContractExpired(int playerId)
	{
		if (CurrentTarget == playerId)
		{
			PlayerState playerState = LatentPlayers.Get(CurrentTarget);
			Utils.DisplayHelpText("The hit contract on " + playerState.ColorNameAndId + " has ~r~expired~s~.");
			Cleanup();
		}
		DispatchScript.HitmanDispatch.RemovePlayer(playerId);
	}

	private void OnJobChangedEvent(object sender, JobArgs e)
	{
		Cleanup();
		if (e.CurrentJobEnum == JobsEnum.Hitman)
		{
			FetchActiveContracts();
		}
	}

	private async void FetchActiveContracts()
	{
		List<KillContractInfo> contracts = (await TriggerServerEventAsync<string>("gtacnr:hitman:getAllActiveContracts", new object[0])).Unjson<List<KillContractInfo>>();
		DispatchScript.HitmanDispatch.ResetMenu();
		DispatchScript.HitmanDispatch.UpdateFromRetrievedContracts(contracts);
	}

	[Command("settarget")]
	private void SetTargetCommand(string[] args)
	{
		if (int.TryParse(args[0], out var result))
		{
			SetTarget(result);
		}
	}
}
