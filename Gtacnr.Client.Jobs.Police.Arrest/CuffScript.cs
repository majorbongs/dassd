using System;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Gtacnr.Client.Anticheat;
using Gtacnr.Client.API;
using Gtacnr.Client.API.Scripts;
using Gtacnr.Client.API.UI;
using Gtacnr.Client.Weapons;
using Gtacnr.Localization;
using Gtacnr.Model;
using Gtacnr.Model.Enums;

namespace Gtacnr.Client.Jobs.Police.Arrest;

public class CuffScript : Script
{
	private readonly Control KEYBOARD_CONTROL_ARREST = (Control)252;

	private static bool canCuff;

	private static bool canTicket;

	private static bool canOpenArrestMenu;

	private static bool canHoldInCustody;

	private static bool cuffActionsEnabled;

	private static bool suspectResisted;

	private static bool isRagdollingAfterSuspectResist;

	public static Player TargetPlayer;

	public static bool IsCuffingOrUncuffing;

	public static CuffScript Instance { get; private set; }

	public CuffScript()
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		Instance = this;
		Gtacnr.Client.API.Jobs.JobChangedEvent += OnJobChangedEvent;
	}

	[Update]
	private async Coroutine RefreshTick()
	{
		await Script.Wait(100);
		bool flag = canCuff;
		bool flag2 = canTicket;
		bool flag3 = canOpenArrestMenu;
		bool flag4 = canHoldInCustody;
		try
		{
			bool num = Gtacnr.Client.API.Jobs.CachedJobEnum.IsPolice();
			TargetPlayer = null;
			canCuff = false;
			canTicket = false;
			canOpenArrestMenu = false;
			canHoldInCustody = false;
			if (!num || IsCuffingOrUncuffing || Game.PlayerPed.IsInVehicle() || ((Entity)Game.PlayerPed).IsDead || Game.PlayerPed.IsRagdoll || ((Entity)Game.PlayerPed).IsInAir || Game.PlayerPed.IsJumping || isRagdollingAfterSuspectResist || Game.PlayerPed.IsGettingUp)
			{
				return;
			}
			Vector3 position = ((Entity)Game.PlayerPed).Position;
			float num2 = 10f.Square();
			bool flag5 = false;
			bool flag6 = false;
			bool flag7 = false;
			foreach (Player player in ((BaseScript)this).Players)
			{
				if (player.Handle == Game.Player.Handle || API.IsPlayerDead(player.Handle))
				{
					continue;
				}
				PlayerState playerState = LatentPlayers.Get(player.ServerId);
				if (playerState == null)
				{
					continue;
				}
				bool canBeCuffed = playerState.CanBeCuffed;
				bool canBeStopped = playerState.CanBeStopped;
				bool isCuffed = playerState.IsCuffed;
				bool isInCustody = playerState.IsInCustody;
				bool ghostMode = playerState.GhostMode;
				if (!((!canBeCuffed && !isCuffed && !canBeStopped) || ghostMode) && !playerState.AdminDuty)
				{
					_ = ((PoolObject)player.Character).Handle;
					Vector3 position2 = ((Entity)player.Character).Position;
					float num3 = ((Vector3)(ref position)).DistanceToSquared(position2);
					if (num3 < num2)
					{
						num2 = num3;
						TargetPlayer = player;
						flag5 = isCuffed;
						flag6 = isInCustody;
						flag7 = canBeStopped && !canBeCuffed;
					}
				}
			}
			if (!(TargetPlayer != (Player)null))
			{
				return;
			}
			int handle = ((PoolObject)TargetPlayer.Character).Handle;
			if (API.IsPedStill(handle) && !API.IsPedShooting(handle) && !API.IsPlayerFreeAiming(TargetPlayer.Handle) && !API.IsPedInCombat(handle, API.PlayerPedId()) && !API.IsPedInMeleeCombat(handle) && (API.IsPedFacingPed(((PoolObject)Game.PlayerPed).Handle, handle, 90f) || API.IsPedBeingStunned(handle, 0)))
			{
				Vector3 entityCoords = API.GetEntityCoords(handle, true);
				float num4 = ((Vector3)(ref position)).DistanceToSquared(entityCoords);
				if (flag7)
				{
					canTicket = num4 < 6.25f;
				}
				else if (!flag5)
				{
					canCuff = num4 < 1.5625f && !API.IsPedInAnyVehicle(handle, true);
				}
				else if (flag6)
				{
					canOpenArrestMenu = num4 < 6.25f;
				}
				else if (!flag6)
				{
					canHoldInCustody = num4 < 6.25f;
				}
			}
		}
		finally
		{
			if (canCuff || canOpenArrestMenu || canHoldInCustody || canTicket)
			{
				bool refresh = flag != canCuff || flag3 != canOpenArrestMenu || flag4 != canHoldInCustody || flag2 != canTicket;
				EnableArrestActions(refresh);
			}
			if (!canCuff && !canOpenArrestMenu && !canHoldInCustody && !canTicket)
			{
				DisableArrestActions();
			}
		}
	}

	private static bool CanCuffPlayer(Player target)
	{
		//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d9: Unknown result type (might be due to invalid IL or missing references)
		if (IsCuffingOrUncuffing || Game.PlayerPed.IsInVehicle() || ((Entity)Game.PlayerPed).IsDead)
		{
			return false;
		}
		if (target == (Player)null)
		{
			return false;
		}
		PlayerState playerState = LatentPlayers.Get(target.ServerId);
		if (playerState == null)
		{
			return false;
		}
		int handle = ((PoolObject)target.Character).Handle;
		if (!API.IsPedStill(handle) || API.IsPedShooting(handle) || API.IsPlayerFreeAiming(target.Handle) || API.IsPedInCombat(handle, API.PlayerPedId()) || API.IsPedInMeleeCombat(handle))
		{
			return false;
		}
		if (!API.IsPedFacingPed(((PoolObject)Game.PlayerPed).Handle, handle, 90f) && !API.IsPedBeingStunned(handle, 0))
		{
			return false;
		}
		bool canBeCuffed = playerState.CanBeCuffed;
		bool isCuffed = playerState.IsCuffed;
		bool isInCustody = playerState.IsInCustody;
		if (!canBeCuffed || isCuffed || isInCustody)
		{
			return false;
		}
		Vector3 position = ((Entity)Game.PlayerPed).Position;
		Vector3 entityCoords = API.GetEntityCoords(handle, true);
		if (((Vector3)(ref position)).DistanceToSquared(entityCoords) > 1.5625f || API.IsPedInAnyVehicle(handle, true))
		{
			return false;
		}
		return true;
	}

	public static async void OrderToFollow()
	{
		int targetServerId = TargetPlayer.ServerId;
		Ped character = TargetPlayer.Character;
		if (character.IsInVehicle())
		{
			Utils.DisplayHelpText("The ~o~suspect ~s~is in a vehicle.");
			return;
		}
		Vector3 position = ((Entity)Game.PlayerPed).Position;
		if (((Vector3)(ref position)).DistanceToSquared(((Entity)character).Position) > 400f)
		{
			Utils.DisplayHelpText("The ~o~suspect ~s~is ~r~too far~s~.");
			return;
		}
		switch (await Instance.TriggerServerEventAsync<int>("gtacnr:police:startFollowing", new object[1] { targetServerId }))
		{
		case 0:
			Utils.DisplayHelpText(LocalizationController.S(Entries.Main.UNEXPECTED_ERROR));
			break;
		case 2:
			Utils.DisplayHelpText("~r~The suspect is not cuffed.");
			break;
		case 3:
			Utils.DisplayHelpText("~r~A transport unit has been called for the suspect.");
			break;
		default:
			Utils.DisplayHelpText($"You ordered ~o~{await Authentication.GetAccountName(targetServerId)} ({targetServerId}) ~s~to follow you.");
			break;
		}
	}

	private async void OnJobChangedEvent(object sender, JobArgs e)
	{
		if (e.PreviousJobEnum.IsPolice() && !(TargetPlayer == (Player)null))
		{
			dynamic val = TargetPlayer.State.Get("gtacnr:police:copImFollowing");
			if (!e.CurrentJobEnum.IsPolice() && val == Game.Player.ServerId)
			{
				await Instance.TriggerServerEventAsync<bool>("gtacnr:police:stopFollowing", new object[1] { TargetPlayer.ServerId });
			}
		}
	}

	public static async void OrderToStopFollowing()
	{
		int targetServerId = TargetPlayer.ServerId;
		if (!(await Instance.TriggerServerEventAsync<bool>("gtacnr:police:stopFollowing", new object[1] { targetServerId })))
		{
			Utils.DisplayHelpText(LocalizationController.S(Entries.Main.UNEXPECTED_ERROR));
		}
		else
		{
			Utils.DisplayHelpText($"You ordered ~o~{await Authentication.GetAccountName(targetServerId)} ({targetServerId}) ~s~to stop following you.");
		}
	}

	public static async void OrderToEnterVehicle()
	{
		int targetServerId = TargetPlayer.ServerId;
		if (TargetPlayer.Character.IsInVehicle())
		{
			Utils.DisplayHelpText("The ~o~suspect ~s~is already in a vehicle.");
			return;
		}
		Vehicle lastVehicle = Game.PlayerPed.LastVehicle;
		if ((Entity)(object)lastVehicle == (Entity)null)
		{
			Utils.DisplayHelpText("You don't have a ~b~police vehicle~s~.");
			return;
		}
		if (!Gtacnr.Utils.IsVehicleModelAPoliceVehicle(Model.op_Implicit(((Entity)lastVehicle).Model)) && !Gtacnr.Utils.IsVehicleModelAnArmoredEmergencyVehicle(Model.op_Implicit(((Entity)lastVehicle).Model)))
		{
			Utils.DisplayHelpText("You need a ~p~police vehicle ~s~to transport a suspect to jail.");
			return;
		}
		if (!lastVehicle.Doors.HasDoor((VehicleDoorIndex)3))
		{
			Utils.DisplayHelpText("You need a ~p~vehicle ~s~with rear seats to transport a suspect.");
			return;
		}
		if (!API.IsVehicleSeatFree(((PoolObject)lastVehicle).Handle, 2))
		{
			Utils.DisplayHelpText("Your ~b~vehicle's back seat ~s~is occupied.");
			return;
		}
		Vector3 position = ((Entity)Game.PlayerPed).Position;
		if (((Vector3)(ref position)).DistanceToSquared(((Entity)lastVehicle).Position) > 400f)
		{
			Utils.DisplayHelpText("Your ~b~vehicle ~s~is ~r~too far~s~.");
			return;
		}
		switch (await Instance.TriggerServerEventAsync<int>("gtacnr:police:forceEnterVehicle", new object[3]
		{
			targetServerId,
			((Entity)lastVehicle).NetworkId,
			0
		}))
		{
		case 0:
			Utils.DisplayHelpText(LocalizationController.S(Entries.Main.UNEXPECTED_ERROR));
			break;
		case 2:
			Utils.DisplayHelpText("~r~The suspect is not cuffed.");
			break;
		case 3:
			Utils.DisplayHelpText("~r~A transport unit has been called for the suspect.");
			break;
		default:
			Utils.DisplayHelpText($"You ordered ~o~{await Authentication.GetAccountName(targetServerId)} ({targetServerId}) ~s~to enter your ~b~vehicle~s~.");
			break;
		}
	}

	public static async void OrderToExitVehicle()
	{
		int targetServerId = TargetPlayer.ServerId;
		if (!TargetPlayer.Character.IsInVehicle())
		{
			Utils.DisplayHelpText("The ~o~suspect ~s~is not in a vehicle.");
		}
		else if (!(await Instance.TriggerServerEventAsync<bool>("gtacnr:police:forceExitVehicle", new object[1] { targetServerId })))
		{
			Utils.DisplayHelpText(LocalizationController.S(Entries.Main.UNEXPECTED_ERROR));
		}
		else
		{
			Utils.DisplayHelpText($"You ordered ~o~{await Authentication.GetAccountName(targetServerId)} ({targetServerId}) ~s~to exit the vehicle.");
		}
	}

	private void EnableArrestActions(bool refresh = false)
	{
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_011e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0102: Unknown result type (might be due to invalid IL or missing references)
		if (cuffActionsEnabled && !refresh)
		{
			return;
		}
		cuffActionsEnabled = true;
		if (refresh)
		{
			Utils.RemoveInstructionalButton("arrestAction");
			KeysScript.DetachListener(KEYBOARD_CONTROL_ARREST, OnKeyEvent);
		}
		string text = (canCuff ? LocalizationController.S(Entries.Player.MENU_PLAYERMENU_CUFF).Replace("~b~", "") : (canOpenArrestMenu ? LocalizationController.S(Entries.Player.MENU_PLAYERMENU_ARREST).Replace("~b~", "") : (canHoldInCustody ? LocalizationController.S(Entries.Player.MENU_PLAYERMENU_CUSTODY).Replace("~b~", "") : (canTicket ? LocalizationController.S(Entries.Player.MENU_PLAYERMENU_GIVE_TICKET).Replace("~b~", "") : ""))));
		if (!string.IsNullOrEmpty(text))
		{
			if (Utils.IsUsingKeyboard())
			{
				Utils.AddInstructionalButton("arrestAction", new InstructionalButton(text ?? "", 2, KEYBOARD_CONTROL_ARREST));
			}
			else
			{
				Utils.RemoveInstructionalButton("arrestAction");
			}
			KeysScript.AttachListener(KEYBOARD_CONTROL_ARREST, OnKeyEvent, 100);
		}
	}

	private void DisableArrestActions()
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		if (cuffActionsEnabled)
		{
			cuffActionsEnabled = false;
			Utils.RemoveInstructionalButton("arrestAction");
			KeysScript.DetachListener(KEYBOARD_CONTROL_ARREST, OnKeyEvent);
		}
	}

	private bool OnKeyEvent(Control control, KeyEventType eventType, InputType inputType)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		if (control == KEYBOARD_CONTROL_ARREST && eventType == KeyEventType.JustPressed && inputType == InputType.Keyboard)
		{
			if (canCuff)
			{
				Cuff();
			}
			else if (canOpenArrestMenu)
			{
				ArrestMenuScript.ShowArrestMenu();
			}
			else if (canHoldInCustody)
			{
				HoldInCustody();
			}
			else if (canTicket)
			{
				GiveTicket();
			}
			return true;
		}
		return false;
	}

	public static async Task<bool> Cuff()
	{
		if (TargetPlayer == (Player)null || !CanCuffPlayer(TargetPlayer))
		{
			Utils.PlayErrorSound();
			return false;
		}
		IsCuffingOrUncuffing = true;
		((Entity)Game.PlayerPed).IsPositionFrozen = true;
		int targetServerId = TargetPlayer.ServerId;
		Ped targetPed = TargetPlayer.Character;
		PlayerState targetInfo = LatentPlayers.Get(targetServerId);
		Utils.DisplayHelpText(LocalizationController.S(Entries.Jobs.POLICE_CUFFING_SUSPECT, targetInfo.ColorNameAndId), playSound: false);
		try
		{
			Vehicle lastVehicle = targetPed.LastVehicle;
			if ((Entity)(object)lastVehicle != (Entity)null && lastVehicle.Exists())
			{
				BaseScript.TriggerServerEvent("gtacnr:mechanic:setVehicleTowable", new object[2]
				{
					((Entity)lastVehicle).NetworkId,
					2
				});
			}
			Game.PlayerPed.Weapons.Select((WeaponHash)(-1569615261));
			if (await Instance.TriggerServerEventAsync<bool>("gtacnr:police:beginCuff", new object[1] { targetServerId }))
			{
				API.TaskTurnPedToFaceEntity(API.PlayerPedId(), ((PoolObject)targetPed).Handle, 1000);
				await BaseScript.Delay(1000);
				while (API.IsPedBeingStunned(((PoolObject)targetPed).Handle, 0))
				{
					await BaseScript.Delay(100);
				}
				bool abort = false;
				int prevHealth = ((Entity)Game.PlayerPed).Health;
				AnimationFlags val = (AnimationFlags)0;
				Game.PlayerPed.Task.PlayAnimation("mp_arresting", "a_uncuff", 4f, 4000, val);
				int i = 0;
				while (i < 32)
				{
					await BaseScript.Delay(125);
					if (!((Entity)(object)targetPed == (Entity)null) && !((Entity)(object)Game.PlayerPed == (Entity)null) && ((Entity)Game.PlayerPed).Health >= prevHealth)
					{
						Vector3 position = ((Entity)targetPed).Position;
						if (!(((Vector3)(ref position)).DistanceToSquared(((Entity)Game.PlayerPed).Position) > 6.25f) && !suspectResisted)
						{
							i++;
							continue;
						}
					}
					abort = true;
					Game.PlayerPed.Task.ClearAllImmediately();
					((Entity)Game.PlayerPed).IsPositionFrozen = false;
					if (!suspectResisted)
					{
						BaseScript.TriggerServerEvent("gtacnr:police:cancelCuff", new object[1] { targetServerId });
						Utils.SendNotification("~r~You have failed to cuff " + targetInfo.NameAndId + ".");
						break;
					}
					suspectResisted = false;
					Game.PlayerPed.Task.ClearAllImmediately();
					Game.PlayerPed.Ragdoll(1500, (RagdollType)0);
					WeaponBehaviorScript.BlockWeaponSwitchingById("resistingCop");
					isRagdollingAfterSuspectResist = true;
					await BaseScript.Delay(1500);
					WeaponBehaviorScript.UnblockWeaponSwitchingById("resistingCop");
					isRagdollingAfterSuspectResist = false;
					break;
				}
				if (!abort)
				{
					Sex freemodePedSex = Utils.GetFreemodePedSex(Game.PlayerPed);
					int index = Preferences.PoliceVoiceIdx.Get();
					string voice = PoliceVoices.GetVoice(freemodePedSex, index);
					if (await Instance.TriggerServerEventAsync<bool>("gtacnr:police:endCuff", new object[3] { targetServerId, "ARREST_PLAYER", voice }))
					{
						Utils.DisplayHelpText("You have cuffed " + targetInfo.ColorNameAndId);
						await Utils.ShakeGamepad();
					}
					else
					{
						Utils.DisplayErrorMessage(48);
					}
				}
			}
			else
			{
				await BaseScript.Delay(1000);
				Utils.DisplayHelpText(targetInfo.ColorNameAndId + " was already cuffed.");
			}
		}
		catch (Exception exception)
		{
			Instance.Print(exception);
		}
		finally
		{
			IsCuffingOrUncuffing = false;
			((Entity)Game.PlayerPed).IsPositionFrozen = false;
		}
		ArrestMenuScript.HideArrestMenu();
		return true;
	}

	public static async void Uncuff()
	{
		if (!(TargetPlayer == (Player)null) && !IsCuffingOrUncuffing)
		{
			IsCuffingOrUncuffing = true;
			int serverId = TargetPlayer.ServerId;
			PlayerState targetInfo = LatentPlayers.Get(serverId);
			Utils.DisplayHelpText("You are uncuffing " + targetInfo.ColorNameAndId, playSound: false);
			Game.PlayerPed.Weapons.Select((WeaponHash)(-1569615261));
			if (await Instance.TriggerServerEventAsync<bool>("gtacnr:police:uncuff", new object[1] { serverId }))
			{
				await BaseScript.Delay(1000);
				Utils.DisplayHelpText("You have uncuffed " + targetInfo.ColorNameAndId);
				await Utils.ShakeGamepad();
			}
			else
			{
				await BaseScript.Delay(1000);
				Utils.DisplayHelpText(targetInfo.ColorNameAndId + " was already uncuffed.");
			}
			ArrestMenuScript.HideArrestMenu();
			IsCuffingOrUncuffing = false;
		}
	}

	private async void HoldInCustody()
	{
		if (!(TargetPlayer == (Player)null) && !IsCuffingOrUncuffing)
		{
			IsCuffingOrUncuffing = true;
			int targetServerId = TargetPlayer.ServerId;
			string name = await Authentication.GetAccountName(targetServerId);
			if (await TriggerServerEventAsync<bool>("gtacnr:police:holdInCustody", new object[1] { targetServerId }))
			{
				Utils.DisplayHelpText($"You are now holding ~o~{name} ({targetServerId}) ~s~in custody.");
				await Utils.ShakeGamepad();
			}
			else
			{
				Utils.DisplayHelpText($"~o~{name} ({targetServerId}) ~s~is already in police custody.");
			}
			ArrestMenuScript.HideArrestMenu();
			DisableArrestActions();
			IsCuffingOrUncuffing = false;
		}
	}

	public static void HoldSuspectInCustody(Player player)
	{
		TargetPlayer = player;
		Instance.HoldInCustody();
	}

	private async void GiveTicket()
	{
		if (TargetPlayer == (Player)null || !canTicket)
		{
			Utils.PlayErrorSound();
			return;
		}
		IsCuffingOrUncuffing = true;
		((Entity)Game.PlayerPed).IsPositionFrozen = true;
		int targetServerId = TargetPlayer.ServerId;
		Ped targetPed = TargetPlayer.Character;
		PlayerState targetInfo = LatentPlayers.Get(targetServerId);
		Utils.DisplayHelpText(LocalizationController.S(Entries.Jobs.POLICE_GIVING_TICKET, targetInfo.ColorNameAndId), playSound: false);
		try
		{
			if (await Instance.TriggerServerEventAsync<bool>("gtacnr:police:beginGiveTicket", new object[1] { targetServerId }))
			{
				while (API.IsPedBeingStunned(((PoolObject)targetPed).Handle, 0))
				{
					await BaseScript.Delay(100);
				}
				bool abort = false;
				int prevHealth = ((Entity)Game.PlayerPed).Health;
				Game.PlayerPed.Weapons.Select((WeaponHash)(-1569615261));
				int notebookBone = API.GetPedBoneIndex(((PoolObject)Game.PlayerPed).Handle, 18905);
				Prop notebookProp = await World.CreateProp(Model.op_Implicit("prop_notepad_01"), ((Entity)Game.PlayerPed).Position, true, false);
				float[] array = new float[6] { 0.1f, 0.02f, 0.05f, 10f, 0f, 0f };
				API.AttachEntityToEntity(((PoolObject)notebookProp).Handle, ((PoolObject)Game.PlayerPed).Handle, notebookBone, array[0], array[1], array[2], array[3], array[4], array[5], true, true, false, true, 1, true);
				int pencilBone = API.GetPedBoneIndex(((PoolObject)Game.PlayerPed).Handle, 58866);
				Prop pencilProp = await World.CreateProp(Model.op_Implicit("prop_pencil_01"), ((Entity)Game.PlayerPed).Position, true, false);
				float[] array2 = new float[6] { 0.11f, -0.02f, 0.001f, -120f, 0f, 0f };
				API.AttachEntityToEntity(((PoolObject)pencilProp).Handle, ((PoolObject)Game.PlayerPed).Handle, pencilBone, array2[0], array2[1], array2[2], array2[3], array2[4], array2[5], true, true, false, true, 1, true);
				AntiEntitySpawnScript.RegisterEntities((Entity)notebookProp, (Entity)pencilProp);
				AnimationFlags val = (AnimationFlags)51;
				Game.PlayerPed.Task.PlayAnimation("missheistdockssetup1clipboard@base", "base", 4f, 4000, val);
				Vector3 startPos = ((Entity)Game.PlayerPed).Position;
				int i = 0;
				while (i < 32)
				{
					await BaseScript.Delay(125);
					Vector3 position;
					if (!((Entity)(object)targetPed == (Entity)null) && !((Entity)(object)Game.PlayerPed == (Entity)null) && ((Entity)Game.PlayerPed).Health >= prevHealth)
					{
						position = ((Entity)targetPed).Position;
						if (!(((Vector3)(ref position)).DistanceToSquared(((Entity)Game.PlayerPed).Position) > 6.25f))
						{
							i++;
							continue;
						}
					}
					abort = true;
					Game.PlayerPed.Task.ClearAllImmediately();
					((Entity)Game.PlayerPed).IsPositionFrozen = false;
					position = ((Entity)targetPed).Position;
					bool flag = ((Vector3)(ref position)).DistanceToSquared(startPos) > 6.25f;
					BaseScript.TriggerServerEvent("gtacnr:police:cancelGiveTicket", new object[2] { targetServerId, flag });
					Utils.SendNotification(LocalizationController.S(Entries.Jobs.POLICE_GIVING_TICKET_FAILED, targetInfo.NameAndId));
					break;
				}
				Game.PlayerPed.Task.ClearAnimation("missheistdockssetup1clipboard@base", "base");
				((PoolObject)notebookProp).Delete();
				((PoolObject)pencilProp).Delete();
				if (!abort)
				{
					int num = await Instance.TriggerServerEventAsync<int>("gtacnr:police:endGiveTicket", new object[1] { targetServerId });
					if (num > 0)
					{
						Utils.DisplayHelpText(LocalizationController.S(Entries.Jobs.POLICE_GIVING_TICKET_SUCCEEDED, targetInfo.ColorNameAndId, num.ToCurrencyString()));
					}
					else
					{
						Utils.DisplayErrorMessage(48);
					}
				}
			}
			else
			{
				Utils.DisplayHelpText(targetInfo.ColorNameAndId + " has already received a fine.");
			}
		}
		catch (Exception exception)
		{
			Instance.Print(exception);
		}
		finally
		{
			IsCuffingOrUncuffing = false;
			((Entity)Game.PlayerPed).IsPositionFrozen = false;
		}
	}

	public static void GiveTicket(Player player)
	{
		TargetPlayer = player;
		Instance.GiveTicket();
	}

	[EventHandler("gtacnr:police:suspectResisted")]
	private void OnSuspectResisted()
	{
		suspectResisted = true;
	}
}
