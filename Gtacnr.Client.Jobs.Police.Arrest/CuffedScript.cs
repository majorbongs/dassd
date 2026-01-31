using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Gtacnr.Client.API;
using Gtacnr.Client.API.Scripts;
using Gtacnr.Client.Businesses.PoliceStations;
using Gtacnr.Client.Characters.Lifecycle;
using Gtacnr.Client.IMenu;
using Gtacnr.Client.Jobs.Hitman;
using Gtacnr.Client.Weapons;
using Gtacnr.Model;
using Gtacnr.Model.Enums;

namespace Gtacnr.Client.Jobs.Police.Arrest;

public class CuffedScript : Script
{
	private Prop handcuffsProp;

	private int copToFollowId;

	private bool wasCuffedLastFrame;

	private bool wasBeingCuffedOrUncuffedLastFrame;

	private bool isBeingStunned;

	private bool canResist;

	private int cuffResistance;

	private int neededResistance;

	private List<DateTime> resistanceTimestamps = new List<DateTime>();

	private DateTime lastResistAttemptT;

	private readonly int[] controlsToDisable1 = new int[9] { 22, 24, 25, 36, 37, 140, 141, 142, 143 };

	private readonly int[] controlsToDisable2 = new int[9] { 23, 30, 31, 32, 33, 34, 35, 44, 75 };

	private bool cancelCuff;

	public static bool IsInCustody { get; private set; }

	public static bool IsCuffed { get; private set; }

	public static bool IsBeingCuffedOrUncuffed { get; private set; }

	public static bool IsBeingCuffed
	{
		get
		{
			if (IsBeingCuffedOrUncuffed)
			{
				return !IsCuffed;
			}
			return false;
		}
	}

	public static bool IsBeingUncuffed
	{
		get
		{
			if (IsBeingCuffedOrUncuffed)
			{
				return IsCuffed;
			}
			return false;
		}
	}

	public CuffedScript()
	{
		DeathScript.Respawning += OnRespawning;
	}

	[Update]
	private async Coroutine StunnedUpdate()
	{
		await Script.Wait(50);
		API.SetPedMinGroundTimeForStungun(API.PlayerPedId(), 500);
		if (API.IsPedBeingStunned(API.PlayerPedId(), 0))
		{
			if (await Gtacnr.Client.API.Crime.GetWantedLevel() > 1)
			{
				await Utils.ShakeGamepad(40, 300);
			}
			if (!isBeingStunned)
			{
				isBeingStunned = true;
			}
		}
		else if (isBeingStunned)
		{
			isBeingStunned = false;
			if (IsCuffed)
			{
				await Script.Wait(1500);
				Game.PlayerPed.Task.ClearAll();
				AnimationFlags val = (AnimationFlags)49;
				string text = "mp_arresting";
				await Game.PlayerPed.Task.PlayAnimation(text, "idle", 4f, -4f, -1, val, 0f);
			}
		}
	}

	private void ResetCuffResistanceState()
	{
		canResist = false;
		cuffResistance = 0;
		resistanceTimestamps.Clear();
		neededResistance = new Random().Next(8, 20);
	}

	private void ResistTask()
	{
		if (!Game.IsControlJustPressed(2, (Control)24) && !Game.IsDisabledControlJustPressed(2, (Control)24))
		{
			return;
		}
		cuffResistance++;
		if (cuffResistance < neededResistance)
		{
			resistanceTimestamps.Add(DateTime.UtcNow);
			Game.PlaySound("3_2_1", "HUD_MINI_GAME_SOUNDSET");
		}
		if (cuffResistance != neededResistance)
		{
			return;
		}
		List<int> list = new List<int>();
		int num = -1;
		foreach (DateTime resistanceTimestamp in resistanceTimestamps)
		{
			num++;
			if (num != 0)
			{
				DateTime dateTime = resistanceTimestamps[num - 1];
				int item = (resistanceTimestamp - dateTime).TotalMilliseconds.ToInt();
				list.Add(item);
			}
		}
		int num2 = 0;
		for (int i = 0; i < list.Count; i++)
		{
			int num3 = list[i];
			for (int j = 0; j < list.Count; j++)
			{
				if (i < j)
				{
					int num4 = list[j];
					if (num4 - 3 < num3 && num3 < num4 + 3)
					{
						num2++;
						break;
					}
				}
			}
		}
		TimeSpan timeSpan = resistanceTimestamps.Last() - resistanceTimestamps.First();
		double num5 = (double)neededResistance / timeSpan.TotalSeconds;
		lastResistAttemptT = DateTime.UtcNow;
		BaseScript.TriggerServerEvent("gtacnr:police:resistArrest", new object[3] { cuffResistance, num2, num5 });
	}

	[Update]
	private async Coroutine CuffedTask()
	{
		if (IsBeingCuffed && canResist)
		{
			ResistTask();
		}
		if (IsCuffed || IsBeingCuffedOrUncuffed)
		{
			if (!wasBeingCuffedOrUncuffedLastFrame)
			{
				WeaponBehaviorScript.BlockWeaponSwitchingByDistinctId("cuffed");
			}
			if (IsCuffed)
			{
				AnimationFlags val = (AnimationFlags)49;
				if (!API.IsEntityPlayingAnim(API.PlayerPedId(), "mp_arresting", "idle", (int)val))
				{
					Game.PlayerPed.Task.ClearAll();
					await Game.PlayerPed.Task.PlayAnimation("mp_arresting", "idle", 4f, -4f, -1, val, 0f);
				}
				API.SetPedMoveRateOverride(((PoolObject)Game.PlayerPed).Handle, 0.8f);
			}
			int[] array = controlsToDisable1;
			foreach (int num in array)
			{
				API.DisableControlAction(1, num, true);
			}
			if (IsInCustody || IsBeingCuffedOrUncuffed)
			{
				array = controlsToDisable2;
				foreach (int num2 in array)
				{
					API.DisableControlAction(1, num2, true);
				}
			}
			if (API.IsPedClimbing(API.PlayerPedId()))
			{
				Game.PlayerPed.Task.ClearAll();
			}
			if (API.IsPedInAnyVehicle(API.PlayerPedId(), false))
			{
				API.DisableControlAction(0, 59, true);
			}
		}
		else if (!IsCuffed && (wasCuffedLastFrame || wasBeingCuffedOrUncuffedLastFrame))
		{
			Game.PlayerPed.Task.ClearAll();
			foreach (int item in controlsToDisable1.Concat(controlsToDisable2))
			{
				API.EnableControlAction(1, item, true);
			}
			WeaponBehaviorScript.UnblockWeaponSwitchingById("cuffed");
		}
		wasCuffedLastFrame = IsCuffed;
		wasBeingCuffedOrUncuffedLastFrame = IsBeingCuffedOrUncuffed;
	}

	[Update]
	private async Coroutine BackInCustodyTick()
	{
		await Script.Wait(1000);
		if (IsCuffed && !IsInCustody && API.IsPedBeingStunned(((PoolObject)Game.PlayerPed).Handle, 0))
		{
			IsInCustody = true;
			Utils.DisplayHelpText("You've been stunned by the ~b~police~s~. You're going nowhere!");
			BaseScript.TriggerServerEvent("gtacnr:police:stunnedToTakeCustody", new object[0]);
		}
	}

	[Update]
	private async Coroutine FollowTask()
	{
		await Script.Wait(500);
		int? arrestingOfficerId = Game.Player.State["gtacnr:police:arrestingOfficer"] as int?;
		Vector3 position;
		if (arrestingOfficerId.HasValue)
		{
			Player val = ((IEnumerable<Player>)((BaseScript)this).Players).FirstOrDefault((Player p) => p.ServerId == arrestingOfficerId.Value);
			if (val == (Player)null || (Entity)(object)val.Character == (Entity)null)
			{
				Game.Player.State.Set("gtacnr:police:arrestingOfficer", (object)null, true);
			}
			else
			{
				PlayerState playerState = LatentPlayers.Get(arrestingOfficerId.Value);
				bool isDead = ((Entity)val.Character).IsDead;
				position = ((Entity)val.Character).Position;
				bool flag = ((Vector3)(ref position)).DistanceToSquared(((Entity)Game.PlayerPed).Position) > 400f;
				if (isDead || flag || !playerState.JobEnum.IsPolice())
				{
					Game.Player.State.Set("gtacnr:police:arrestingOfficer", (object)null, true);
				}
			}
		}
		if (copToFollowId == 0)
		{
			return;
		}
		Ped val2 = new Ped(API.GetPlayerPed(API.GetPlayerFromServerId(copToFollowId)));
		if ((Entity)(object)val2 == (Entity)null || !val2.Exists())
		{
			StopFollowing();
			return;
		}
		position = ((Entity)val2).Position;
		float num = ((Vector3)(ref position)).DistanceToSquared(((Entity)Game.PlayerPed).Position);
		if (num > 900f)
		{
			StopFollowing();
		}
		else if (num < 6.25f)
		{
			if (Game.PlayerPed.IsWalking)
			{
				Game.PlayerPed.Task.ClearAll();
			}
		}
		else if (!Game.PlayerPed.IsWalking)
		{
			Game.PlayerPed.Task.GoTo((Entity)(object)val2);
		}
		void StopFollowing()
		{
			SetCopToFollow(0);
			Game.PlayerPed.Task.ClearAll();
		}
	}

	[EventHandler("gtacnr:police:onHeldInCustody")]
	private async void OnHeldInCustody(int officerServerId)
	{
		if (officerServerId != 0)
		{
			Utils.DisplayHelpText($"Officer ~b~{await Authentication.GetAccountName(officerServerId)} ({officerServerId}) ~s~is now holding you in custody.");
			Game.Player.State.Set("gtacnr:police:arrestingOfficer", (object)officerServerId, true);
			await Utils.ShakeGamepad();
		}
		IsInCustody = true;
	}

	[EventHandler("gtacnr:police:beginGetCuffed")]
	private async void OnBeginGetCuffed(int officerServerId)
	{
		if (!IsBeingCuffedOrUncuffed)
		{
			WardrobeMenuScript.CloseAllMenus();
			IsBeingCuffedOrUncuffed = true;
			Game.PlayerPed.Task.ClearAllImmediately();
			ResetCuffResistanceState();
			canResist = Gtacnr.Utils.CheckTimePassed(lastResistAttemptT, 15000.0);
			string text = (canResist ? " Press ~INPUT_ATTACK~ repeatedly to resist." : "");
			PlayerState playerState = LatentPlayers.Get(officerServerId);
			Utils.DisplayHelpText("Officer " + playerState.ColorNameAndId + " is cuffing you." + text, playSound: false);
			Ped val = new Ped(API.GetPlayerPed(API.GetPlayerFromServerId(officerServerId)));
			Vector3 val2 = ((Entity)val).Position + ((Entity)val).ForwardVector;
			API.TaskTurnPedToFaceCoord(API.PlayerPedId(), val2.X, val2.Y, val2.Z, 1000);
			await BaseScript.Delay(1000);
			await BeginGetCuffed();
			if (IsBeingCuffedOrUncuffed)
			{
				((Entity)Game.PlayerPed).IsPositionFrozen = true;
			}
		}
	}

	[EventHandler("gtacnr:police:endGetCuffed")]
	private async void OnEndGetCuffed(int officerServerId)
	{
		if (IsBeingCuffedOrUncuffed)
		{
			WardrobeMenuScript.CloseAllMenus();
			await EndGetCuffed(officerServerId);
			await Utils.ShakeGamepad();
			PlayerState playerState = LatentPlayers.Get(officerServerId);
			Utils.DisplayHelpText("Officer " + playerState.ColorNameAndId + " has cuffed you.", playSound: false);
			Game.Player.State.Set("gtacnr:police:arrestingOfficer", (object)officerServerId, true);
			((Entity)Game.PlayerPed).IsPositionFrozen = false;
			IsBeingCuffedOrUncuffed = false;
		}
	}

	[EventHandler("gtacnr:police:cancelGetCuffed")]
	private async void OnCancelGetCuffed(int officerServerId, bool resisted)
	{
		if (IsBeingCuffedOrUncuffed)
		{
			CancelGetCuffed();
			await Utils.ShakeGamepad();
			PlayerState playerState = LatentPlayers.Get(officerServerId);
			Utils.DisplayHelpText("Officer " + playerState.ColorNameAndId + " has failed to cuff you.", playSound: false);
			Game.Player.State.Set("gtacnr:police:arrestingOfficer", (object)null, true);
			((Entity)Game.PlayerPed).IsPositionFrozen = false;
			IsBeingCuffedOrUncuffed = false;
			if (resisted)
			{
				Game.PlayerPed.Task.PlayAnimation("avoids", "frback_tofront", 4f, 1500, (AnimationFlags)0);
				WeaponBehaviorScript.BlockWeaponSwitchingById("resistingCrim");
				await BaseScript.Delay(2500);
				WeaponBehaviorScript.UnblockWeaponSwitchingById("resistingCrim");
			}
		}
	}

	[EventHandler("gtacnr:police:bribeAccepted")]
	private async void OnBribeAccepted()
	{
		if (IsBeingCuffedOrUncuffed)
		{
			CancelGetCuffed();
			await Utils.ShakeGamepad();
			Game.Player.State.Set("gtacnr:police:arrestingOfficer", (object)null, true);
			((Entity)Game.PlayerPed).IsPositionFrozen = false;
			IsBeingCuffedOrUncuffed = false;
		}
	}

	[EventHandler("gtacnr:police:onUncuffed")]
	private async void OnUncuffed(int uncuffPlayerId)
	{
		if (!IsBeingCuffedOrUncuffed)
		{
			IsBeingCuffedOrUncuffed = true;
			if (uncuffPlayerId != 0)
			{
				PlayerState playerInfo = LatentPlayers.Get(uncuffPlayerId);
				string officerStr = (playerInfo.JobEnum.IsPolice() ? "Officer " : "");
				Utils.DisplayHelpText($"{officerStr}{playerInfo} ~s~is uncuffing you", playSound: false);
				GetUncuffed();
				await BaseScript.Delay(1000);
				await Utils.ShakeGamepad();
				Utils.DisplayHelpText($"{officerStr}{playerInfo} ~s~uncuffed you", playSound: false);
				Chat.AddMessage(Gtacnr.Utils.Colors.Info, $"{officerStr}{playerInfo} uncuffed you.");
			}
			else
			{
				GetUncuffed();
			}
			IsBeingCuffedOrUncuffed = false;
			Game.Player.State.Set("gtacnr:police:arrestingOfficer", (object)null, true);
			((Entity)Game.PlayerPed).IsPositionFrozen = false;
		}
	}

	[EventHandler("gtacnr:police:autoCuff")]
	private async void OnAutoCuff()
	{
		Game.PlayerPed.Task.ClearAllImmediately();
		await GetCuffed();
		Game.Player.State.Set("gtacnr:police:arrestingOfficer", (object)null, true);
	}

	[EventHandler("gtacnr:police:onForcedToEnterVehicle")]
	private async void OnForcedToEnterVehicle(int vehicleId, int pedNetId, int officerId)
	{
		if (pedNetId != 0)
		{
			Ped val = new Ped(API.NetToPed(pedNetId));
			if ((Entity)(object)val != (Entity)null)
			{
				val.PlayAmbientSpeech("ARREST_PLAYER", (SpeechModifier)3);
			}
			Utils.DisplayHelpText("The ~b~police officers ~s~ordered you to get in the ~b~vehicle~s~.");
			Game.Player.State.Set("gtacnr:police:inTransportUnitCustody", (object)true, true);
		}
		else if (officerId != 0)
		{
			Utils.DisplayHelpText($"~b~Officer {await Authentication.GetAccountName(officerId)} ({officerId}) ~s~ordered you to get in the ~b~vehicle~s~!");
		}
		SetCopToFollow(0);
		EnterVehicleForced(vehicleId, pedNetId != 0);
	}

	[EventHandler("gtacnr:police:onForcedToExitVehicle")]
	private async void OnForcedToExitVehicle(int officerId)
	{
		if (officerId != 0)
		{
			Utils.DisplayHelpText($"~b~Officer {await Authentication.GetAccountName(officerId)} ({officerId}) ~s~ordered you to exit the ~b~vehicle~s~.");
		}
		Game.PlayerPed.Task.LeaveVehicle((LeaveVehicleFlags)256);
	}

	[EventHandler("gtacnr:police:onStartFollowing")]
	private async void OnStartFollowing(int officerServerId)
	{
		Utils.DisplayHelpText($"~b~Officer {await Authentication.GetAccountName(officerServerId)} ({officerServerId}) ~s~ordered you to follow them.");
		SetCopToFollow(officerServerId);
	}

	[EventHandler("gtacnr:police:onStopFollowing")]
	private async void OnStopFollowing(int officerServerId)
	{
		Utils.DisplayHelpText($"~b~Officer {await Authentication.GetAccountName(officerServerId)} ({officerServerId}) ~s~ordered you to stop following.");
		SetCopToFollow(0);
	}

	[EventHandler("gtacnr:police:onArrested")]
	private async void OnArrested(int officerServerId)
	{
		await Utils.FadeOut();
		GetUncuffed();
		SetCopToFollow(0);
		PoliceStation randomClosePoliceStation = PoliceStationsScript.GetRandomClosePoliceStation(((Entity)Game.PlayerPed).Position, exceptIfTooClose: true);
		if (randomClosePoliceStation == null)
		{
			Print("An unexpected error has occurred while trying to spawn at the closest police station");
			await Utils.FadeIn();
			BaseScript.TriggerServerEvent("gtacnr:police:onArrestCompleted", new object[1] { officerServerId });
			return;
		}
		await Utils.TeleportToCoords(randomClosePoliceStation.ReleaseLocation.XYZ(), randomClosePoliceStation.ReleaseLocation.W, Utils.TeleportFlags.PlaceOnGround);
		SurrenderScript.IsSurrendered = false;
		Game.PlayerPed.Task.ClearAllImmediately();
		await Utils.FadeIn();
		await BaseScript.Delay(1000);
		BaseScript.TriggerServerEvent("gtacnr:police:onArrestCompleted", new object[1] { officerServerId });
		Game.Player.State.Set("gtacnr:police:inTransportUnitCustody", (object)false, true);
		PlayerState officerInfo = LatentPlayers.Get(officerServerId);
		await BaseScript.Delay(5000);
		int num = LatentPlayers.All.Count((PlayerState ls) => ls.JobEnum == JobsEnum.Hitman);
		if (num != 0 && !((Entity)Game.PlayerPed).IsDead && LatentPlayers.Get(officerInfo.Id) != null)
		{
			Game.PlaySound("UNDER_THE_BRIDGE", "HUD_AWARDS");
			await InteractiveNotificationsScript.Show($"Wanna make <C>{officerInfo.NameAndId}</C> regret arresting you? There are <C>{num} hitmen</C> online that can help you.", InteractiveNotificationType.Notification, OnAccepted, TimeSpan.FromSeconds(7.0), 0u, "Hit Contract", "Hit Contract (hold)", null, Gtacnr.Utils.Colors.HudYellowDark);
		}
		bool OnAccepted()
		{
			Utils.PlaySelectSound();
			HitmanContractMenuScript.ShowMenu(null, onSite: false, officerInfo.Id);
			return true;
		}
	}

	[EventHandler("gtacnr:police:onTransportCalled")]
	private async void OnTransportCalled(int officerServerId, int vehicleNetId)
	{
		PlayerState playerState = LatentPlayers.Get(officerServerId);
		Utils.DisplayHelpText("Officer " + playerState.ColorNameAndId + " called a transport unit for you.");
		Chat.AddMessage(Gtacnr.Utils.Colors.Info, $"{playerState} called a transport unit for you.");
		int num = API.NetToVeh(vehicleNetId);
		if (num != 0)
		{
			Vehicle vehicle = new Vehicle(num);
			Blip blip = ((Entity)vehicle).AttachBlip();
			API.SetBlipDisplay(((PoolObject)blip).Handle, 8);
			blip.Sprite = (BlipSprite)56;
			blip.Scale = 1f;
			blip.Color = (BlipColor)3;
			blip.Name = "Transport Unit";
			DateTime t = DateTime.UtcNow;
			do
			{
				await BaseScript.Delay(1000);
			}
			while ((!vehicle.Exists() || !Game.PlayerPed.IsInVehicle(vehicle)) && !Gtacnr.Utils.CheckTimePassed(t, 60000.0));
			if (((PoolObject)blip).Exists())
			{
				((PoolObject)blip).Delete();
			}
		}
	}

	[EventHandler("gtacnr:police:unlockHandcuffs")]
	private void OnUnlockHandcuffs()
	{
		GetUncuffed();
	}

	[EventHandler("gtacnr:police:stopBeingInCustody")]
	private async void OnStopSurrendering()
	{
		IsInCustody = false;
		SetCopToFollow(0);
		Game.PlayerPed.Task.ClearAll();
		Utils.DisplayHelpText("The ~b~cops ~s~are not watching you, it's time to go! Use a ~y~lockpick ~s~to unlock your handcuffs.");
		Game.Player.State.Set("gtacnr:police:arrestingOfficer", (object)null, true);
		if ((Entity)(object)Game.PlayerPed.CurrentVehicle != (Entity)null && (Entity)(object)Game.PlayerPed.CurrentVehicle.Driver != (Entity)(object)Game.PlayerPed)
		{
			Game.PlayerPed.Task.LeaveVehicle((LeaveVehicleFlags)4096);
		}
		await Utils.ShakeGamepad();
	}

	private void OnRespawning(object sender, EventArgs e)
	{
		if (IsCuffed)
		{
			GetUncuffed();
		}
		if (IsBeingCuffedOrUncuffed)
		{
			IsBeingCuffedOrUncuffed = false;
			IsInCustody = false;
			IsCuffed = false;
			((Entity)Game.PlayerPed).IsPositionFrozen = false;
		}
	}

	[EventHandler("gtacnr:died")]
	private void OnDied()
	{
		if (IsCuffed)
		{
			GetUncuffed();
		}
	}

	private async void EnterVehicleForced(int vehicleNetId, bool waitForDoorToOpen = false)
	{
		if (!API.NetworkDoesEntityExistWithNetworkId(vehicleNetId))
		{
			return;
		}
		int vehicleId = API.NetworkGetEntityFromNetworkId(vehicleNetId);
		if (vehicleId == 0)
		{
			return;
		}
		Vehicle vehicle = new Vehicle(vehicleId);
		if (!vehicle.Exists())
		{
			return;
		}
		Vector3 entryPositionOfDoor = API.GetEntryPositionOfDoor(((PoolObject)vehicle).Handle, 3);
		Game.PlayerPed.Task.GoTo(entryPositionOfDoor, true, 10000);
		DateTime t = DateTime.UtcNow;
		Vector3 position;
		if (waitForDoorToOpen)
		{
			while (API.GetVehicleDoorAngleRatio(vehicleId, 3) < 0.1f)
			{
				await BaseScript.Delay(1000);
				if (!Gtacnr.Utils.CheckTimePassed(t, 15000.0))
				{
					continue;
				}
				if (vehicle.Exists())
				{
					position = ((Entity)vehicle).Position;
					if (((Vector3)(ref position)).DistanceToSquared(((Entity)Game.PlayerPed).Position) < 20f)
					{
						Game.PlayerPed.Task.WarpIntoVehicle(vehicle, (VehicleSeat)2);
					}
				}
				return;
			}
		}
		else
		{
			vehicle.LockStatus = (VehicleLockStatus)1;
			API.SetVehicleDoorsLockedForAllPlayers(((PoolObject)vehicle).Handle, false);
			API.SetVehicleDoorOpen(((PoolObject)vehicle).Handle, 3, false, false);
		}
		await BaseScript.Delay(2000);
		if (!vehicle.Exists())
		{
			return;
		}
		Game.PlayerPed.Task.EnterVehicle(vehicle, (VehicleSeat)2, -1, 0f, 0);
		t = DateTime.UtcNow;
		while (!Gtacnr.Utils.CheckTimePassed(t, 10000.0))
		{
			await BaseScript.Delay(1000);
			if (!vehicle.Exists())
			{
				return;
			}
			if (Game.PlayerPed.IsInVehicle(vehicle))
			{
				break;
			}
		}
		if (!Game.PlayerPed.IsInVehicle(vehicle))
		{
			position = ((Entity)vehicle).Position;
			if (((Vector3)(ref position)).DistanceToSquared(((Entity)Game.PlayerPed).Position) < 20f)
			{
				Game.PlayerPed.Task.WarpIntoVehicle(vehicle, (VehicleSeat)2);
			}
		}
		API.SetVehicleDoorShut(((PoolObject)vehicle).Handle, 3, false);
	}

	private void SetCopToFollow(int copServerId)
	{
		copToFollowId = copServerId;
		Game.Player.State.Set("gtacnr:police:copImFollowing", (object)copServerId, true);
	}

	private async Task BeginGetCuffed()
	{
		if (SurrenderScript.IsSurrendered)
		{
			SurrenderScript.IsSurrendered = false;
			AnimationFlags val = (AnimationFlags)2;
			Game.PlayerPed.Task.PlayAnimation("random@arrests", "kneeling_arrest_get_up", 4f, -1, val);
			await BaseScript.Delay(3000);
		}
		if (cancelCuff)
		{
			cancelCuff = false;
			return;
		}
		while (API.IsPedBeingStunned(API.PlayerPedId(), 0))
		{
			await BaseScript.Delay(100);
		}
		if (cancelCuff)
		{
			cancelCuff = false;
			return;
		}
		AnimationFlags val2 = (AnimationFlags)49;
		Game.PlayerPed.Task.PlayAnimation("mp_arresting", "idle", 4f, -1, val2);
		int num = API.PlayerPedId();
		API.SetEnableHandcuffs(num, true);
		Utils.SetPedConfigFlagEx(num, PedConfigFlag.DisableLadderClimbing, value: true);
		API.SetCurrentPedWeapon(num, 2725352035u, true);
	}

	private async Task EndGetCuffed(int officerServerId = 0)
	{
		if (cancelCuff)
		{
			cancelCuff = false;
			return;
		}
		using (DisposableModel propModel = new DisposableModel(Model.op_Implicit("p_cs_cuffs_02_s")))
		{
			await propModel.Load();
			if ((Entity)(object)handcuffsProp != (Entity)null)
			{
				((PoolObject)handcuffsProp).Delete();
			}
			handcuffsProp = new Prop(API.CreateObject(Model.op_Implicit(propModel.Model), 0f, 0f, 0f, true, true, false));
			PedBone val = Game.PlayerPed.Bones[(Bone)6286];
			API.AttachEntityToEntity(((PoolObject)handcuffsProp).Handle, ((PoolObject)Game.PlayerPed).Handle, EntityBone.op_Implicit((EntityBone)(object)val), -0.03f, 0.07f, 0f, 100.25f, -25.95f, 259.9f, true, true, false, true, 1, true);
		}
		if (cancelCuff)
		{
			cancelCuff = false;
			return;
		}
		IsCuffed = true;
		IsInCustody = true;
		BaseScript.TriggerEvent("gtacnr:police:gotCuffed", new object[1] { officerServerId });
	}

	private void CancelGetCuffed()
	{
		cancelCuff = true;
		Game.PlayerPed.Task.ClearAll();
		if (IsBeingCuffedOrUncuffed)
		{
			((Entity)Game.PlayerPed).IsPositionFrozen = false;
		}
		int num = API.PlayerPedId();
		API.SetEnableHandcuffs(num, false);
		Utils.SetPedConfigFlagEx(num, PedConfigFlag.DisableLadderClimbing, value: false);
	}

	private async Task GetCuffed()
	{
		await BeginGetCuffed();
		await EndGetCuffed();
	}

	private void GetUncuffed()
	{
		Game.PlayerPed.Task.ClearAll();
		int num = API.PlayerPedId();
		API.SetEnableHandcuffs(num, false);
		Utils.SetPedConfigFlagEx(num, PedConfigFlag.DisableLadderClimbing, value: false);
		if ((Entity)(object)handcuffsProp != (Entity)null)
		{
			((PoolObject)handcuffsProp).Delete();
		}
		IsCuffed = false;
		IsInCustody = false;
		SetCopToFollow(0);
		BaseScript.TriggerEvent("gtacnr:police:gotUncuffed", new object[0]);
	}
}
