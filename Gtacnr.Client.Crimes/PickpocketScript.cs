using System;
using System.Collections.Generic;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Gtacnr.Client.Admin;
using Gtacnr.Client.API;
using Gtacnr.Client.API.Scripts;
using Gtacnr.Client.API.UI;
using Gtacnr.Client.Businesses;
using Gtacnr.Client.Characters.Lifecycle;
using Gtacnr.Client.Communication;
using Gtacnr.Client.Crimes.Robberies.Jewelry;
using Gtacnr.Client.HUD;
using Gtacnr.Client.Items;
using Gtacnr.Client.Jobs.Police.Arrest;
using Gtacnr.Client.Zones;
using Gtacnr.Data;
using Gtacnr.Model;
using Gtacnr.ResponseCodes;
using NativeUI;

namespace Gtacnr.Client.Crimes;

public class PickpocketScript : Script
{
	private bool areControlsEnabled;

	private bool isBusy;

	private Ped closestPed;

	private List<Ped> nearbyPeds = new List<Ped>();

	private TextTimerBar stolenTimerBar;

	private Blip theftAreaBlip;

	private int currentTheftAmount;

	private int currentTheftTargetId;

	private int currentThiefId;

	private bool policeCalled;

	private readonly float THEFT_AREA_RANGE = 300f;

	public static PickpocketScript Instance { get; private set; }

	public PickpocketScript()
	{
		Instance = this;
		DeathEventScript.PlayerDeath += OnPlayerDeath;
	}

	private void OnPlayerDeath(object sender, DeathEventArgs e)
	{
		if (e.VictimId == currentThiefId)
		{
			SetCurrentThief(0);
		}
	}

	public static int GetCurrentThief(bool ignoreIfCopsCalled = true)
	{
		if (ignoreIfCopsCalled && Instance.policeCalled)
		{
			return 0;
		}
		return Instance.currentThiefId;
	}

	private void SetCurrentThief(int thiefId)
	{
		currentThiefId = thiefId;
	}

	public static void RegisterPoliceCalled()
	{
		Instance.policeCalled = true;
	}

	[Update]
	private async Coroutine CacheNearbyPedsTask()
	{
		if ((Entity)(object)Game.PlayerPed == (Entity)null)
		{
			return;
		}
		await Script.Wait(5000);
		Vector3 position = ((Entity)Game.PlayerPed).Position;
		float num = 3600f;
		nearbyPeds.Clear();
		Ped[] allPeds = World.GetAllPeds();
		foreach (Ped val in allPeds)
		{
			if (((Vector3)(ref position)).DistanceToSquared(((Entity)val).Position) < num)
			{
				nearbyPeds.Add(val);
			}
		}
	}

	[Update]
	private async Coroutine ClosestPedUpdateTask()
	{
		if (nearbyPeds.Count == 0)
		{
			return;
		}
		await Script.Wait(500);
		CheckIfInTheftArea();
		Ped val = null;
		if (Gtacnr.Client.API.Jobs.CachedJobEnum.IsPublicService() || Gtacnr.Data.Jobs.GetJobData(Gtacnr.Client.API.Jobs.CachedJob) == null || CuffedScript.IsCuffed || SurrenderScript.IsSurrendered || SurrenderScript.IsHoldingHandsUp || ModeratorMenuScript.IsOnDuty || NoClipScript.IsNoClipActive || DrugScript.IsOverdosing || DeathScript.IsAlive != true || Game.PlayerPed.IsSwimming || Game.PlayerPed.IsFalling || JewelryRobberyStateScript.IsPlayerRobbing)
		{
			if (theftAreaBlip != (Blip)null && currentTheftAmount == 0)
			{
				((PoolObject)theftAreaBlip).Delete();
				theftAreaBlip = null;
			}
		}
		else
		{
			Vector3 position = ((Entity)Game.PlayerPed).Position;
			float num = 1f;
			foreach (Ped nearbyPed in nearbyPeds)
			{
				if (((PoolObject)nearbyPed).Handle == ((PoolObject)Game.PlayerPed).Handle || nearbyPed.IsInVehicle() || ((Entity)nearbyPed).IsDead || nearbyPed.IsInCombat || nearbyPed.IsInMeleeCombat || nearbyPed.IsFleeing || API.GetPedType(((PoolObject)nearbyPed).Handle) == 28 || API.GetPedAlertness(((PoolObject)nearbyPed).Handle) > 0 || (API.IsEntityAMissionEntity(((PoolObject)nearbyPed).Handle) && !nearbyPed.IsPlayer))
				{
					continue;
				}
				if (nearbyPed.IsPlayer)
				{
					int num2 = API.NetworkGetPlayerIndexFromPed(((PoolObject)nearbyPed).Handle);
					int playerServerId = API.GetPlayerServerId(num2);
					new Player(num2);
					PlayerState playerState = LatentPlayers.Get(playerServerId);
					bool isCuffed = playerState.IsCuffed;
					if (playerState.JobEnum.IsPublicService() || playerState.AdminDuty || isCuffed || nearbyPed.IsRunning || nearbyPed.IsSprinting || ((Entity)nearbyPed).IsInWater || nearbyPed.IsFalling || nearbyPed.IsInParachuteFreeFall || SafezoneScript.GetSafezoneAtCoords(((Entity)nearbyPed).Position) != null || SpawnScript.GetSpawnLocationAtCoords(((Entity)nearbyPed).Position) != null || BusinessScript.GetBusinessEmployeeAtCoords(((Entity)nearbyPed).Position) != null)
					{
						continue;
					}
				}
				float num3 = ((Vector3)(ref position)).DistanceToSquared(((Entity)nearbyPed).Position);
				if (num3 < num)
				{
					num = num3;
					val = nearbyPed;
				}
			}
		}
		if ((Entity)(object)closestPed == (Entity)null && (Entity)(object)val != (Entity)null)
		{
			EnableControls();
		}
		else if ((Entity)(object)closestPed != (Entity)null && (Entity)(object)val == (Entity)null)
		{
			DisableControls();
		}
		closestPed = val;
	}

	private void EnableControls()
	{
		if (!areControlsEnabled)
		{
			areControlsEnabled = true;
			Utils.AddInstructionalButton("pickpocketSteal", new Gtacnr.Client.API.UI.InstructionalButton("Steal", 2, (Control)29));
			KeysScript.AttachListener((Control)29, OnKeyEvent, 10);
		}
	}

	private void DisableControls()
	{
		if (areControlsEnabled)
		{
			areControlsEnabled = false;
			Utils.RemoveInstructionalButton("pickpocketSteal");
			KeysScript.DetachListener((Control)29, OnKeyEvent);
		}
	}

	private bool OnKeyEvent(Control control, KeyEventType eventType, InputType inputType)
	{
		if (eventType == KeyEventType.JustPressed && (Entity)(object)closestPed != (Entity)null)
		{
			StealFromClosestPed();
			PointScript.StopPointing();
			return true;
		}
		return false;
	}

	private void StealFromClosestPed()
	{
		if ((Entity)(object)closestPed == (Entity)null)
		{
			return;
		}
		try
		{
			if (isBusy)
			{
				Utils.PlayErrorSound();
				return;
			}
			isBusy = true;
			int handle = ((PoolObject)closestPed).Handle;
			if (API.IsPedAPlayer(handle))
			{
				int playerServerId = API.GetPlayerServerId(API.NetworkGetPlayerIndexFromPed(handle));
				PlayerState playerState = LatentPlayers.Get(playerServerId);
				if (playerState.JobEnum.IsPublicService())
				{
					string text = Gtacnr.Data.Jobs.GetJobData(playerState.Job)?.Name ?? "N/A";
					Utils.DisplayHelpText("You cannot pickpocket a ~b~" + text + "~s~.");
				}
				else if (PartyScript.IsInParty && PartyScript.PartyMembers.Contains(playerServerId))
				{
					Utils.DisplayHelpText("You cannot pickpocket a fellow ~y~party member~s~.");
				}
				else
				{
					StealFromPlayer(playerServerId);
				}
			}
			else
			{
				StealFromNPC(handle);
			}
		}
		catch (Exception exception)
		{
			Print(exception);
		}
		finally
		{
			isBusy = false;
		}
	}

	private async void StealFromNPC(int pedId)
	{
		if (!API.NetworkGetEntityIsNetworked(pedId))
		{
			return;
		}
		PickpocketResponse response = (await TriggerServerEventAsync<string>("gtacnr:crimes:pickpocket:stealFromNPC", new object[1] { API.NetworkGetNetworkIdFromEntity(pedId) })).Unjson<PickpocketResponse>();
		if (response.Code == PickpocketResponseCode.SamePed)
		{
			Utils.DisplayHelpText("You cannot attempt to steal from the ~r~same person ~s~twice.");
		}
		else if (response.Code == PickpocketResponseCode.Cooldown)
		{
			Utils.DisplayHelpText("You must wait before attempting to steal from a ~r~pedestrian ~s~again.");
		}
		else if (response.Code == PickpocketResponseCode.TooFar)
		{
			Utils.DisplayHelpText("You are ~r~too far ~s~from the pedestrian.");
		}
		else if (response.Code == PickpocketResponseCode.Success)
		{
			if (response.AmountStolen <= 0)
			{
				Utils.DisplaySubtitle("You ~r~failed ~s~to steal anything.", 2000);
			}
			else
			{
				Utils.DisplaySubtitle("You stole a ~g~wallet ~s~containing ~g~" + response.AmountStolen.ToCurrencyString() + "~s~.", 3000);
			}
			if (!response.Alerted)
			{
				return;
			}
			if (response.Reported && !response.Aggressive)
			{
				API.ClearPedTasks(pedId);
				await BaseScript.Delay(300);
				API.TaskTurnPedToFaceEntity(pedId, API.PlayerPedId(), 5000);
				API.PlayAmbientSpeech1(pedId, "GENERIC_INSULT_MED", "Speech_Params_Force_Shouted");
				await BaseScript.Delay(3000);
				API.TaskUseMobilePhone(pedId, 1);
				await BaseScript.Delay(1000);
				API.PlayAmbientSpeech1(pedId, "PHONE_CALL_COPS", "Speech_Params_Force_Shouted");
				await BaseScript.Delay(5000);
				API.ClearPedTasks(pedId);
				API.TaskWanderStandard(pedId, 10f, 10);
				return;
			}
			API.ClearPedTasks(pedId);
			await BaseScript.Delay(300);
			int randomInt = Gtacnr.Utils.GetRandomInt();
			if (randomInt > 85)
			{
				API.GiveWeaponToPed(pedId, 1593441988u, 40, false, true);
			}
			else if (randomInt > 70)
			{
				API.GiveWeaponToPed(pedId, 3219281620u, 40, false, true);
			}
			if (response.Aggressive)
			{
				API.GiveWeaponToPed(pedId, 3675956304u, 100, false, true);
			}
			uint bestPedWeapon = (uint)API.GetBestPedWeapon(pedId, false);
			if (bestPedWeapon != 0 && bestPedWeapon != 2725352035u)
			{
				API.SetCurrentPedWeapon(pedId, bestPedWeapon, true);
			}
			API.SetPedFleeAttributes(pedId, 0, true);
			API.SetPedCombatAttributes(pedId, 5, true);
			API.SetPedCombatAttributes(pedId, 46, true);
			API.SetPedCombatAbility(pedId, 1);
			if (!response.Aggressive)
			{
				API.TaskGotoEntityAiming(pedId, API.PlayerPedId(), 2f, 15f);
				API.PlayAmbientSpeech1(pedId, "GENERIC_INSULT_HIGH", "Speech_Params_Force_Shouted");
				await BaseScript.Delay(2500);
				API.PlayAmbientSpeech1(pedId, "CHALLENGE_THREATEN", "Speech_Params_Force_Shouted");
				await BaseScript.Delay(2500);
			}
			else
			{
				API.PlayAmbientSpeech1(pedId, "CHALLENGE_THREATEN", "Speech_Params_Force_Shouted");
			}
			API.TaskCombatPed(pedId, API.PlayerPedId(), 0, 16);
		}
		else
		{
			Utils.DisplayErrorMessage(151, (int)response.Code);
		}
	}

	private async void StealFromPlayer(int playerId)
	{
		if (SafezoneScript.GetSafezoneAtCoords(((Entity)Game.PlayerPed).Position) != null)
		{
			Utils.DisplayHelpText("You cannot pickpocket a player in a ~g~Safe Zone~s~.");
			return;
		}
		if (DeathScript.HasSpawnProtection)
		{
			BaseScript.TriggerEvent("gtacnr:disableSpawnProtection", new object[0]);
			BaseScript.TriggerServerEvent("gtacnr:spawnProtectionDisabled", new object[0]);
		}
		PickpocketResponse pickpocketResponse = (await TriggerServerEventAsync<string>("gtacnr:crimes:pickpocket:stealFromPlayer", new object[1] { playerId })).Unjson<PickpocketResponse>();
		PlayerState playerState = LatentPlayers.Get(playerId);
		if (pickpocketResponse.Code == PickpocketResponseCode.SamePed)
		{
			Utils.DisplayHelpText(playerState.ColorNameAndId + " has been ~r~recently ~s~pickpocketed.");
		}
		else if (pickpocketResponse.Code == PickpocketResponseCode.Cooldown)
		{
			Utils.DisplayHelpText("You must wait before attempting to steal from a ~r~player ~s~again.");
		}
		else if (pickpocketResponse.Code == PickpocketResponseCode.TooFar)
		{
			Utils.DisplayHelpText("You are ~r~too far ~s~from " + playerState.ColorNameAndId + ".");
		}
		else if (pickpocketResponse.Code == PickpocketResponseCode.TooManyTimes)
		{
			Utils.DisplayHelpText("You have ~r~stolen ~s~from " + playerState.ColorNameAndId + " too many times in this session.");
		}
		else if (pickpocketResponse.Code == PickpocketResponseCode.AlreadyPickpocketing)
		{
			Utils.DisplayHelpText("You already have ~y~" + pickpocketResponse.AmountStolen.ToCurrencyString() + " ~s~in stolen cash. Get away from the ~o~area ~s~to obtain it.");
		}
		else if (pickpocketResponse.Code == PickpocketResponseCode.Success)
		{
			if (pickpocketResponse.AmountStolen <= 0)
			{
				if (pickpocketResponse.FakeWallet)
				{
					Utils.DisplaySubtitle("You stole a ~r~fake wallet~s~.", 2000);
				}
				else
				{
					Utils.DisplaySubtitle("You ~r~failed ~s~to steal anything.", 2000);
				}
				return;
			}
			Utils.DisplaySubtitle("Leave the ~o~area~s~!");
			Utils.DisplayHelpText("You stole a ~g~wallet ~s~containing ~y~" + pickpocketResponse.AmountStolen.ToCurrencyString() + "~s~. Leave the ~o~area ~s~alive to keep the ~g~money~s~.");
			currentTheftAmount = pickpocketResponse.AmountStolen;
			currentTheftTargetId = playerState.Id;
			stolenTimerBar = new TextTimerBar("WALLET", currentTheftAmount.ToCurrencyString() ?? "")
			{
				TextColor = TextColors.Yellow
			};
			TimerBarScript.AddTimerBar(stolenTimerBar);
			if (theftAreaBlip != (Blip)null)
			{
				((PoolObject)theftAreaBlip).Delete();
			}
			theftAreaBlip = World.CreateBlip(((Entity)Game.PlayerPed).Position, THEFT_AREA_RANGE);
			theftAreaBlip.IsShortRange = false;
			theftAreaBlip.Sprite = (BlipSprite)(-1);
			theftAreaBlip.Color = (BlipColor)47;
			theftAreaBlip.Alpha = 128;
			Utils.SetBlipName(theftAreaBlip, "Theft Area", "theft_area");
			API.SetBlipDisplay(((PoolObject)theftAreaBlip).Handle, 8);
		}
		else
		{
			Utils.DisplayErrorMessage(150, (int)pickpocketResponse.Code);
		}
	}

	[EventHandler("gtacnr:crimes:pickpocket:gotPickpocketed")]
	private void OnGotPickpocketed(int thiefId, int amount, bool reported)
	{
		PlayerState playerState = LatentPlayers.Get(thiefId);
		Utils.DisplayHelpText(playerState.ColorNameAndId + " stole ~r~" + amount.ToCurrencyString() + " ~s~from your wallet." + (reported ? " The ~b~police ~s~has witnessed the crime." : ""));
		Utils.SendNotification(playerState.ColorNameAndId + " stole your wallet. You can ~r~kill ~s~them, call the ~b~police ~s~on them, or both. If they ~r~die ~s~or are ~b~cuffed ~s~before leaving the area, you will get your ~g~money ~s~back.");
		SetCurrentThief(thiefId);
		Utils.ShakeGamepad(2000);
		policeCalled = false;
	}

	[EventHandler("gtacnr:crimes:pickpocket:gotAlmostPickpocketed")]
	private void OnGotAlmostPickpocketed(int thiefId, bool fakeWallet)
	{
		Utils.DisplayHelpText(LatentPlayers.Get(thiefId).ColorNameAndId + " attempted to ~r~steal ~s~your wallet" + (fakeWallet ? " but stole a fake wallet instead." : "."));
	}

	[EventHandler("gtacnr:died")]
	private void OnDead(int killerId, int cause)
	{
		CancelWalletTheftMission();
	}

	[EventHandler("gtacnr:police:gotCuffed")]
	private void OnGotCuffed(int officerId)
	{
		CancelWalletTheftMission();
	}

	private async void CheckIfInTheftArea()
	{
		if (!(theftAreaBlip != (Blip)null))
		{
			return;
		}
		Vector3 position = ((Entity)Game.PlayerPed).Position;
		if (((Vector3)(ref position)).DistanceToSquared(theftAreaBlip.Position) > THEFT_AREA_RANGE * THEFT_AREA_RANGE)
		{
			((PoolObject)theftAreaBlip).Delete();
			theftAreaBlip = null;
			if (stolenTimerBar != null)
			{
				TimerBarScript.RemoveTimerBar(stolenTimerBar);
				stolenTimerBar = null;
			}
			if (await TriggerServerEventAsync<bool>("gtacnr:crimes:pickpocket:succeed", new object[0]))
			{
				PlayerState playerState = LatentPlayers.Get(currentTheftTargetId);
				Utils.DisplayHelpText("You successfully stole ~g~" + currentTheftAmount.ToCurrencyString() + " ~s~from " + playerState.ColorNameAndId + "'s wallet.");
			}
			currentTheftAmount = 0;
			currentTheftTargetId = 0;
		}
	}

	public void CancelWalletTheftMission()
	{
		if (theftAreaBlip != (Blip)null)
		{
			((PoolObject)theftAreaBlip).Delete();
			theftAreaBlip = null;
		}
		if (stolenTimerBar != null)
		{
			TimerBarScript.RemoveTimerBar(stolenTimerBar);
			stolenTimerBar = null;
		}
		currentTheftAmount = 0;
		currentTheftTargetId = 0;
		BaseScript.TriggerServerEvent("gtacnr:crimes:pickpocket:fail", new object[0]);
	}

	[EventHandler("gtacnr:crimes:pickpocket:thiefFailed")]
	private async void OnThiefFailed(int thiefId, int amount)
	{
		await BaseScript.Delay(2000);
		Utils.DisplayHelpText(LatentPlayers.Get(thiefId).ColorNameAndId + " failed to ~r~steal ~s~your wallet. Your ~g~" + amount.ToCurrencyString() + " ~s~have been returned.");
		SetCurrentThief(0);
	}
}
