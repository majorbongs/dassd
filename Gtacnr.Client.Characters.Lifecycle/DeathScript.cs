using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Gtacnr.Client.Admin;
using Gtacnr.Client.Anticheat;
using Gtacnr.Client.API;
using Gtacnr.Client.API.Scripts;
using Gtacnr.Client.API.UI;
using Gtacnr.Client.API.UI.Menus;
using Gtacnr.Client.Characters.Editor;
using Gtacnr.Client.Communication;
using Gtacnr.Client.HUD;
using Gtacnr.Client.Jobs.Hitman;
using Gtacnr.Client.Jobs.Police;
using Gtacnr.Data;
using Gtacnr.Localization;
using Gtacnr.Model;
using Gtacnr.Model.Enums;
using Gtacnr.ResponseCodes;
using MenuAPI;
using NativeUI;

namespace Gtacnr.Client.Characters.Lifecycle;

public class DeathScript : Script
{
	private static DeathScript instance;

	private static readonly int RESPAWN_SECONDS = 60;

	private static readonly int RESPAWN_EXTRA_SECONDS = 60;

	private static readonly string DEATH_TEXT_LABEL = "RESPAWN";

	private List<RespawnLocation> respawns = Gtacnr.Utils.LoadJson<List<RespawnLocation>>("data/respawns.json");

	private TextTimerBar respawnTimerBar;

	private int timeLeftForAutoRespawn;

	private bool isBeingRevived;

	private bool areInstructionsShown;

	private bool isWaitingForHelp;

	private bool emsCheckMode;

	private static DateTime lastEmsPing = DateTime.MinValue;

	private Dictionary<string, Menu> menus = new Dictionary<string, Menu>();

	private Dictionary<string, MenuItem> menuItems = new Dictionary<string, MenuItem>();

	private bool hasTakenRevenge;

	private static bool revengeTransfersEnabled;

	private static bool revengeTargetMarkersEnabled;

	private static bool revengeClaimantMarkersEnabled;

	private bool isBusyTransfering;

	public static bool? IsAlive { get; set; } = null;

	public static bool HasSpawnProtection { get; set; }

	public static int ForceDeathCause { get; set; }

	public static DeathFeedMode DeathFeedMode { get; set; }

	public static bool RevengeTransfersEnabled => revengeTransfersEnabled;

	public static bool RevengeTargetMarkersEnabled => revengeTargetMarkersEnabled;

	public static bool RevengeClaimantMarkersEnabled => revengeClaimantMarkersEnabled;

	public static RevengeData CachedRevengeData { get; private set; }

	public static event EventHandler Respawning;

	public DeathScript()
	{
		DeathEventScript.PlayerDeath += OnPlayerDeath;
		instance = this;
	}

	protected override void OnStarted()
	{
		DeathFeedMode = Preferences.DeathFeedMode.Get();
		revengeTransfersEnabled = Preferences.RevengeTransfers.Get();
		revengeTargetMarkersEnabled = Preferences.RevengeTargetMarkers.Get();
		revengeClaimantMarkersEnabled = Preferences.RevengeClaimantMarkersEnabled.Get();
		if (revengeTransfersEnabled)
		{
			BaseScript.TriggerServerEvent("gtacnr:toggleRevengeTransfers", new object[1] { revengeTransfersEnabled });
		}
		AddRevengeCommandSuggestions();
		CreateRevengeMenus();
	}

	private void ShowRespawnInstructions(bool call911 = true, bool respawn = true, bool pingHelp = true)
	{
		areInstructionsShown = true;
		if (pingHelp)
		{
			Utils.AddInstructionalButton("pingHelp", new Gtacnr.Client.API.UI.InstructionalButton(LocalizationController.S(Entries.Death.BTN_PING_MEDIC), 2, (Control)22));
		}
		if (call911)
		{
			Utils.AddInstructionalButton("deathCall911", new Gtacnr.Client.API.UI.InstructionalButton(LocalizationController.S(Entries.Death.BTN_CALL_911), 2, (Control)201));
		}
		if (respawn)
		{
			Utils.AddInstructionalButton("deathRespawn", new Gtacnr.Client.API.UI.InstructionalButton(LocalizationController.S(Entries.Death.BTN_RESPAWN), 2, (Control)204));
		}
	}

	private void HideRespawnInstructions(bool ignorePingHelp = false)
	{
		areInstructionsShown = false;
		Utils.RemoveInstructionalButton("deathCall911");
		Utils.RemoveInstructionalButton("deathRespawn");
		if (!ignorePingHelp)
		{
			Utils.RemoveInstructionalButton("pingHelp");
		}
	}

	private void ShowRespawnTimer()
	{
		string text = Gtacnr.Utils.SecondsToMinutesAndSeconds(timeLeftForAutoRespawn);
		respawnTimerBar = new TextTimerBar(DEATH_TEXT_LABEL, text);
		TimerBarScript.AddTimerBar(respawnTimerBar);
	}

	private void HideRespawnTimer()
	{
		TimerBarScript.RemoveTimerBar(respawnTimerBar);
		respawnTimerBar = null;
	}

	private async Task Respawn()
	{
		IsAlive = null;
		DeathScript.Respawning(this, EventArgs.Empty);
		BaseScript.TriggerServerEvent("gtacnr:respawning", new object[1] { false });
		await Utils.FadeOut(2000);
		Job jobData = Gtacnr.Data.Jobs.GetJobData(Gtacnr.Client.API.Jobs.CachedJob);
		string spawnJob = jobData.Id;
		if (!jobData.SeparateSpawnLocations)
		{
			spawnJob = "none";
		}
		RespawnLocation respawnLocation = null;
		float num = 12250000f;
		Vector3 position = ((Entity)Game.PlayerPed).Position;
		List<RespawnLocation> list = new List<RespawnLocation>();
		IEnumerable<RespawnLocation> enumerable = respawns.Where((RespawnLocation r) => r.Job == spawnJob);
		foreach (RespawnLocation item in enumerable)
		{
			Vector3 position2 = item.Position;
			float num2 = ((Vector3)(ref position2)).DistanceToSquared(position);
			if (num2 < num && (num2 > 40000f || item.IsCayo))
			{
				list.Add(item);
				if (list.Count == 5)
				{
					break;
				}
			}
		}
		if (list.Count == 0)
		{
			list.AddRange(enumerable.Where((RespawnLocation r) => !r.IsCayo));
		}
		int index = new Random().Next(list.Count);
		respawnLocation = list[index];
		if (respawnLocation == null)
		{
			Print("Fatal Error: Unable to find a suitable respawn location!");
			return;
		}
		Vector3 pos = respawnLocation.Position;
		float hdg = respawnLocation.Heading;
		await Utils.TeleportToCoords(pos, hdg, Utils.TeleportFlags.PlaceOnGround);
		Utils.Freeze();
		API.StopAllAlarms(true);
		lock (AntiHealthLockScript.HealThreadLock)
		{
			AntiHealthLockScript.JustHealed();
			API.NetworkResurrectLocalPlayer(pos.X, pos.Y, pos.Z, hdg, true, true);
			API.ResurrectPed(API.PlayerPedId());
		}
		Game.PlayerPed.ClearBloodDamage();
		Ped[] allPeds = World.GetAllPeds();
		foreach (Ped val in allPeds)
		{
			if (((Entity)val).IsDead && ((Entity)val).Model == ((Entity)Game.PlayerPed).Model)
			{
				((PoolObject)val).Delete();
			}
		}
		DateTime t = DateTime.UtcNow;
		while (!API.HasCollisionLoadedAroundEntity(API.PlayerPedId()) && !Gtacnr.Utils.CheckTimePassed(t, 5000.0))
		{
			await BaseScript.Delay(1);
		}
		Utils.Freeze(freeze: false);
		((Entity)Game.PlayerPed).IsVisible = true;
		((Entity)Game.PlayerPed).ResetOpacity();
		Utils.RemoveAllAttachedProps();
		IsAlive = true;
		BaseScript.TriggerEvent("gtacnr:respawned", new object[0]);
		BaseScript.TriggerServerEvent("gtacnr:respawned", new object[1] { respawnLocation.IsCayo });
		await Utils.FadeIn(2000);
		if (respawnLocation.Radio > 0f && RadioScript.RadioChannel.Frequency != respawnLocation.Radio)
		{
			SuggestRadioChange();
		}
		async void SuggestRadioChange()
		{
			await BaseScript.Delay(5000);
			await InteractiveNotificationsScript.Show(LocalizationController.S(Entries.Death.TUNE_SUGG_FREQ, $"{respawnLocation.Radio:0.000}"), InteractiveNotificationType.HelpText, TuneIn, TimeSpan.FromSeconds(10.0), 0u, LocalizationController.S(Entries.Death.BTN_TUNE_IN), LocalizationController.S(Entries.Death.BTN_TUNE_IN_HOLD));
		}
		bool TuneIn()
		{
			RadioScript.SetChannel(respawnLocation.Radio);
			Utils.DisplayHelpText();
			Utils.PlaySelectSound();
			return true;
		}
	}

	[Update]
	private async Coroutine CheckDeathTask()
	{
		if (!IsAlive.HasValue || !API.IsEntityDead(API.PlayerPedId()))
		{
			return;
		}
		if (IsAlive.Value)
		{
			IsAlive = false;
			Menus.CloseAll();
			await Script.Yield();
			int deathSource = API.GetPedSourceOfDeath(API.PlayerPedId());
			int deathCause = API.GetPedCauseOfDeath(API.PlayerPedId());
			bool flag = false;
			if (API.IsEntityAVehicle(deathSource))
			{
				deathSource = API.GetPedInVehicleSeat(deathSource, -1);
				deathCause = -1553120962;
			}
			if (ForceDeathCause != 0)
			{
				deathCause = ForceDeathCause;
				ForceDeathCause = 0;
			}
			if (API.IsEntityAPed(deathSource))
			{
				flag = API.IsPedAPlayer(deathSource);
				if (flag)
				{
					deathSource = API.GetPlayerServerId(API.NetworkGetPlayerIndexFromPed(deathSource));
					flag = true;
					if (deathSource == Game.Player.ServerId)
					{
						deathSource = 0;
						flag = false;
					}
				}
			}
			if (!flag && deathSource != 0 && API.IsEntityAPed(deathSource))
			{
				deathSource = -255;
				if (API.GetPedType(deathSource) == 28)
				{
					deathSource = -254;
				}
			}
			Vector3 position = ((Entity)Game.PlayerPed).Position;
			string zoneName = Utils.GetLocationName(position);
			int bone = -1;
			API.GetPedLastDamageBone(((PoolObject)Game.PlayerPed).Handle, ref bone);
			BaseScript.TriggerEvent("gtacnr:dying", new object[3] { deathSource, deathCause, zoneName });
			DyingResponse dyingResponse = (DyingResponse)(await TriggerServerEventAsync<int>("gtacnr:dying", new object[3] { deathSource, deathCause, zoneName }));
			BaseScript.TriggerEvent("gtacnr:died", new object[4] { deathSource, deathCause, zoneName, bone });
			BaseScript.TriggerServerEvent("gtacnr:died", new object[4] { deathSource, deathCause, zoneName, bone });
			isBeingRevived = false;
			isWaitingForHelp = false;
			emsCheckMode = false;
			await Script.Wait(3000);
			if (dyingResponse == DyingResponse.ForceRespawn)
			{
				timeLeftForAutoRespawn = 0;
				HideRespawnTimer();
				HideRespawnInstructions();
				await Respawn();
				Utils.SendNotification(LocalizationController.S(Entries.Death.NO_PARAMEDICS));
				return;
			}
			Player closestEMSInMyRange = GetClosestEMSInMyRange();
			if (closestEMSInMyRange == (Player)null)
			{
				timeLeftForAutoRespawn = RESPAWN_SECONDS;
				ShowRespawnTimer();
				ShowRespawnInstructions();
			}
			else
			{
				timeLeftForAutoRespawn = RESPAWN_SECONDS;
				ShowRespawnTimer();
				HideRespawnInstructions();
				isWaitingForHelp = true;
				emsCheckMode = true;
				Utils.SendNotification(LocalizationController.S(Entries.Death.SOME_PARAMEDICS));
				EMSOnScene(closestEMSInMyRange.ServerId);
			}
		}
		if (Game.IsControlJustPressed(2, (Control)204) && !isBeingRevived && !isWaitingForHelp)
		{
			timeLeftForAutoRespawn = 0;
			HideRespawnInstructions();
			HideRespawnTimer();
			await Respawn();
		}
		else if (Game.IsControlJustPressed(2, (Control)201) && !isBeingRevived && !isWaitingForHelp && !emsCheckMode)
		{
			timeLeftForAutoRespawn += RESPAWN_EXTRA_SECONDS;
			BaseScript.TriggerServerEvent("gtacnr:ems:requestHelp", new object[0]);
			HideRespawnInstructions(ignorePingHelp: true);
			isWaitingForHelp = true;
		}
		else if (Game.IsControlJustPressed(2, (Control)22) && !isBeingRevived && isWaitingForHelp)
		{
			if (Gtacnr.Utils.CheckTimePassed(lastEmsPing, 30000.0))
			{
				Utils.PlaySelectSound();
				BaseScript.TriggerServerEvent("gtacnr:ems:pingHelp", new object[0]);
				lastEmsPing = DateTime.UtcNow;
			}
			else
			{
				Utils.DisplayErrorMessage(0, -1, LocalizationController.S(Entries.Death.PING_MEDIC_COOLDOWN, Gtacnr.Utils.CalculateTimeIn(lastEmsPing.AddMilliseconds(30000.0))));
			}
		}
	}

	private Player GetClosestEMSInMyRange()
	{
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		foreach (Player player in ((BaseScript)this).Players)
		{
			PlayerState playerState = LatentPlayers.Get(player);
			if (playerState != null && !((Entity)(object)player.Character == (Entity)null) && !(player == Game.Player) && playerState.WantedLevel <= 1 && !((Entity)player.Character).IsDead && playerState.JobEnum.IsEMSOrFD())
			{
				Vector3 position = ((Entity)player.Character).Position;
				if (((Vector3)(ref position)).DistanceToSquared(((Entity)Game.PlayerPed).Position) <= 65f.Square())
				{
					return player;
				}
			}
		}
		return null;
	}

	[Update]
	private async Coroutine CalculateDeathTimerTask()
	{
		if (timeLeftForAutoRespawn <= 0)
		{
			return;
		}
		await Script.Wait(1000);
		if (emsCheckMode && !isBeingRevived)
		{
			if (GetClosestEMSInMyRange() != (Player)null)
			{
				HideRespawnInstructions(ignorePingHelp: true);
				ShowRespawnInstructions(call911: false, respawn: false);
				isWaitingForHelp = true;
			}
			else
			{
				ShowRespawnInstructions(call911: false, respawn: true, pingHelp: false);
				isWaitingForHelp = false;
			}
		}
		if (isBeingRevived)
		{
			HideRespawnInstructions();
			HideRespawnTimer();
			return;
		}
		if (timeLeftForAutoRespawn <= 0)
		{
			HideRespawnInstructions();
			HideRespawnTimer();
			return;
		}
		timeLeftForAutoRespawn--;
		if (timeLeftForAutoRespawn == 0)
		{
			HideRespawnInstructions();
			HideRespawnTimer();
			await Respawn();
		}
		else if (timeLeftForAutoRespawn > 0)
		{
			if (respawnTimerBar != null)
			{
				respawnTimerBar.Text = Gtacnr.Utils.SecondsToMinutesAndSeconds(timeLeftForAutoRespawn);
			}
			if (timeLeftForAutoRespawn < 20)
			{
				respawnTimerBar.TextColor = Colors.GTARed;
			}
			else
			{
				respawnTimerBar.TextColor = Colors.White;
			}
		}
	}

	[Update]
	private async Coroutine ApplySpawnProtectionTask()
	{
		await Script.Wait(100);
		((Entity)Game.PlayerPed).IsInvincible = HasSpawnProtection || ModeratorMenuScript.IsOnDuty;
	}

	[Update]
	private async Coroutine RemoveSpawnProtectionTask()
	{
		if ((Entity)(object)Game.PlayerPed != (Entity)null && HasSpawnProtection && (Game.IsControlPressed(2, (Control)25) || Game.IsControlPressed(2, (Control)24) || API.IsPedInCombat(((PoolObject)Game.PlayerPed).Handle, 0) || Game.PlayerPed.IsInVehicle() || ((Entity)Game.PlayerPed).IsDead))
		{
			DisableSpawnProtection();
			BaseScript.TriggerServerEvent("gtacnr:spawnProtectionDisabled", new object[0]);
		}
	}

	[EventHandler("gtacnr:enableSpawnProtection")]
	private void EnableSpawnProtection()
	{
		if (!HasSpawnProtection)
		{
			((Entity)Game.PlayerPed).IsInvincible = true;
			HasSpawnProtection = true;
			Utils.SendNotification(LocalizationController.S(Entries.Death.SPAWN_PROTECTION_ON));
		}
	}

	[EventHandler("gtacnr:disableSpawnProtection")]
	private void DisableSpawnProtection()
	{
		if (HasSpawnProtection)
		{
			((Entity)Game.PlayerPed).IsInvincible = false;
			HasSpawnProtection = false;
			Utils.SendNotification(LocalizationController.S(Entries.Death.SPAWN_PROTECTION_OFF));
		}
	}

	private void OnPlayerDeath(object sender, DeathEventArgs e)
	{
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f1: Unknown result type (might be due to invalid IL or missing references)
		if (!SpawnScript.HasSpawned || CharacterCreationScript.IsInCreator)
		{
			return;
		}
		PlayerState playerState = LatentPlayers.Get(e.VictimId);
		PlayerState killerInfo = LatentPlayers.Get(e.KillerId);
		switch (DeathFeedMode)
		{
		case DeathFeedMode.Off:
			return;
		case DeathFeedMode.Proximity:
		{
			Vector3 position = ((Entity)Game.PlayerPed).Position;
			float num = ((Vector3)(ref position)).DistanceToSquared(playerState.Position);
			float num2;
			if (killerInfo != null)
			{
				position = ((Entity)Game.PlayerPed).Position;
				num2 = ((Vector3)(ref position)).DistanceToSquared(killerInfo.Position);
			}
			else
			{
				num2 = float.MaxValue;
			}
			float num3 = num2;
			float num4 = 3600f;
			if (num > num4 && num3 > num4)
			{
				return;
			}
			break;
		}
		}
		if (playerState == null)
		{
			return;
		}
		if (killerInfo == null)
		{
			string text = "<C>" + playerState.ColorNameAndIdWithWantedLevel(e.VictimWantedLevel) + "</C>";
			Utils.SendNotification((e.KillerId == -255) ? LocalizationController.S(Entries.Death.KILLED_BY_NPC, text) : ((e.KillerId == -254) ? LocalizationController.S(Entries.Death.KILLED_BY_ANIMAL, text) : ((e.Cause == -999) ? LocalizationController.S(Entries.Death.KILLED_BY_OD, text) : ((e.Cause == -998) ? LocalizationController.S(Entries.Death.KILLED_BY_DISEASE, text) : ((e.Cause == -10959621 || e.Cause == 1936677264) ? LocalizationController.S(Entries.Death.DROWNED, text) : ((e.Cause == -842959696) ? LocalizationController.S(Entries.Death.DIED_FROM_FALL, text) : ((e.Cause == 539292904) ? LocalizationController.S(Entries.Death.DIED_FROM_EXPLOSION, text) : ((e.Cause == -544306709) ? LocalizationController.S(Entries.Death.DIED_IN_FIRE, text) : LocalizationController.S(Entries.Death.DIED, text)))))))));
		}
		else
		{
			if (killerInfo.Id == Game.Player.ServerId)
			{
				if (e.HitmanContractReward != 0)
				{
					Utils.DisplaySubtitle("~r~Hit contract!", 2000);
					Game.PlaySound("UNDER_THE_BRIDGE", "HUD_AWARDS");
				}
				else if (e.Bounty > 0)
				{
					Utils.DisplaySubtitle("~r~Bounty!", 2000);
					Game.PlaySound("UNDER_THE_BRIDGE", "HUD_AWARDS");
				}
				else if (e.IsRevenge && !e.IsSelfDefense)
				{
					Utils.DisplaySubtitle("~r~Revenge!", 2000);
					Game.PlaySound("UNDER_THE_BRIDGE", "HUD_AWARDS");
					if (!hasTakenRevenge)
					{
						Chat.AddMessage(Gtacnr.Utils.Colors.Info, "ℹ You can kill players with a red circle around their normal blip in revenge. Type /revenge for revenge information.");
						hasTakenRevenge = true;
					}
				}
			}
			if (JurisdictionScript.IsPointOutOfJurisdiction(playerState.Position) && e.Bounty == 0 && e.HitmanContractReward == 0L)
			{
				return;
			}
			string text2 = ((e.Cause == 539292904) ? LocalizationController.S(Entries.Death.WITH_EXPLOSIVES) : ((e.Cause == -1553120962 || e.Cause == 133987706) ? LocalizationController.S(Entries.Death.WITH_VEHICLE) : ((e.Cause == -868994466) ? LocalizationController.S(Entries.Death.WITH_WATER_CANNON) : LocalizationController.S(Entries.Death.WITH_WEAPON))));
			string text3 = "";
			if (API.IsWeaponValid((uint)e.Cause))
			{
				int weaponDamageType = API.GetWeaponDamageType((uint)e.Cause);
				if (e.Cause == API.GetHashKey("weapon_unarmed"))
				{
					text2 = LocalizationController.S(Entries.Death.WITH_FISTS);
				}
				else if (weaponDamageType == 2)
				{
					text2 = ((!new List<WeaponHash>
					{
						(WeaponHash)(-1834847097),
						(WeaponHash)(-1716189206),
						(WeaponHash)(-581044007),
						(WeaponHash)(-538741184),
						(WeaponHash)(-853065399)
					}.Contains((WeaponHash)e.Cause)) ? LocalizationController.S(Entries.Death.WITH_MELEE_WEAPON) : LocalizationController.S(Entries.Death.WITH_BLADE));
				}
				else if (weaponDamageType == 3)
				{
					int weapontypeGroup = API.GetWeapontypeGroup((uint)e.Cause);
					text2 = ((weapontypeGroup != API.GetHashKey("GROUP_PISTOL")) ? ((weapontypeGroup != API.GetHashKey("GROUP_SMG")) ? ((weapontypeGroup != API.GetHashKey("GROUP_MG")) ? ((weapontypeGroup != API.GetHashKey("GROUP_SHOTGUN")) ? ((weapontypeGroup != API.GetHashKey("GROUP_RIFLE")) ? ((weapontypeGroup != API.GetHashKey("GROUP_SNIPER")) ? (e.IsHeadshot ? LocalizationController.S(Entries.Death.WITH_FIREARM_HEADSHOT) : LocalizationController.S(Entries.Death.WITH_FIREARM)) : (e.IsHeadshot ? LocalizationController.S(Entries.Death.WITH_PRECISION_RIFLE_HEADSHOT) : LocalizationController.S(Entries.Death.WITH_PRECISION_RIFLE))) : (e.IsHeadshot ? LocalizationController.S(Entries.Death.WITH_ASSAULT_RIFLE_HEADSHOT) : LocalizationController.S(Entries.Death.WITH_ASSAULT_RIFLE))) : (e.IsHeadshot ? LocalizationController.S(Entries.Death.WITH_SHOTGUN_HEADSHOT) : LocalizationController.S(Entries.Death.WITH_SHOTGUN))) : (e.IsHeadshot ? LocalizationController.S(Entries.Death.WITH_LMG_HEADSHOT) : LocalizationController.S(Entries.Death.WITH_LMG))) : (e.IsHeadshot ? LocalizationController.S(Entries.Death.WITH_SMG_HEADSHOT) : LocalizationController.S(Entries.Death.WITH_SMG))) : ((!new List<WeaponHash>
					{
						(WeaponHash)(-1045183535),
						(WeaponHash)(-879347409),
						(WeaponHash)(-1746263880),
						(WeaponHash)(-1853920116)
					}.Contains((WeaponHash)e.Cause)) ? (e.IsHeadshot ? LocalizationController.S(Entries.Death.WITH_PISTOL_HEADSHOT) : LocalizationController.S(Entries.Death.WITH_PISTOL)) : (e.IsHeadshot ? LocalizationController.S(Entries.Death.WITH_REVOLVER_HEADSHOT) : LocalizationController.S(Entries.Death.WITH_REVOLVER))));
				}
				else
				{
					text2 = weaponDamageType switch
					{
						14 => LocalizationController.S(Entries.Death.WITH_WATER_CANNON), 
						13 => LocalizationController.S(Entries.Death.WITH_GAS), 
						6 => LocalizationController.S(Entries.Death.WITH_FIRE), 
						5 => LocalizationController.S(Entries.Death.WITH_EXPLOSIVES), 
						_ => LocalizationController.S(Entries.Death.WITH_WEAPON), 
					};
				}
			}
			if (e.Bounty > 0 || e.HitmanContractReward != 0)
			{
				text3 = LocalizationController.S(Entries.Death.FOR_BOUNTY, (e.HitmanContractReward + (ulong)e.Bounty).ToCurrencyString());
			}
			else if (e.IsSelfDefense)
			{
				text3 = LocalizationController.S(Entries.Death.IN_SELF_DEFENSE);
			}
			else if (e.IsRevenge)
			{
				text3 = LocalizationController.S(Entries.Death.IN_REVENGE);
			}
			else if (e.IsRedzone)
			{
				text3 = LocalizationController.S(Entries.Death.IN_REDZONE);
			}
			if (e.Bounty >= 50000 || e.HitmanContractReward >= 50000)
			{
				Utils.SendNotification(LocalizationController.S(Entries.Death.KILL_MSG, $"<C>{killerInfo}</C>", $"<C>{playerState}</C>", text2, "<C>" + text3 + "</C>").Trim(' ').RemoveExtraWhiteSpaces()
					.Replace("~r~", "")
					.Replace("~g~", "")
					.Replace("~s~", "") + ".", Gtacnr.Utils.Colors.HudRed);
			}
			else
			{
				Utils.SendNotification(LocalizationController.S(Entries.Death.KILL_MSG, "<C>" + killerInfo.ColorNameAndId + "</C>", "<C>" + playerState.ColorNameAndIdWithWantedLevel(e.VictimWantedLevel) + "</C>", text2, text3).Trim(' ').RemoveExtraWhiteSpaces() + ".");
			}
		}
		if (playerState.Id == Game.Player.ServerId && killerInfo != null)
		{
			AfterRespawn();
		}
		async void AfterRespawn()
		{
			while (((Entity)Game.PlayerPed).IsDead)
			{
				await BaseScript.Delay(1000);
			}
			await BaseScript.Delay(5000);
			int num5 = LatentPlayers.All.Count((PlayerState ls) => ls.JobEnum == JobsEnum.Hitman);
			if (num5 != 0 && !((Entity)Game.PlayerPed).IsDead && LatentPlayers.Get(killerInfo.Id) != null)
			{
				Game.PlaySound("UNDER_THE_BRIDGE", "HUD_AWARDS");
				await InteractiveNotificationsScript.Show($"Wanna make <C>{killerInfo.NameAndId}</C> regret killing you? There are <C>{num5} hitmen</C> online that can help you.", InteractiveNotificationType.Notification, OnAccepted, TimeSpan.FromSeconds(7.0), 0u, "Hit Contract", "Hit Contract (hold)", null, Gtacnr.Utils.Colors.HudYellowDark);
			}
		}
		bool OnAccepted()
		{
			Utils.PlaySelectSound();
			HitmanContractMenuScript.ShowMenu(null, onSite: false, killerInfo.Id);
			return true;
		}
	}

	[EventHandler("gtacnr:getRevived")]
	private async void OnRevived(int medicId, bool isEMS)
	{
		if (!((Entity)(object)Game.PlayerPed == (Entity)null))
		{
			IsAlive = null;
			await Utils.FadeOut();
			Vector3 position = ((Entity)Game.PlayerPed).Position;
			float heading = ((Entity)Game.PlayerPed).Heading;
			((Entity)Game.PlayerPed).Position = ((Entity)Game.PlayerPed).Position - 50f;
			lock (AntiHealthLockScript.HealThreadLock)
			{
				AntiHealthLockScript.JustHealed();
				AntiTeleportScript.JustTeleported();
				API.NetworkResurrectLocalPlayer(position.X, position.Y, position.Z, heading, true, true);
				API.ResurrectPed(API.PlayerPedId());
				((Entity)Game.PlayerPed).Health = 100;
			}
			isBeingRevived = false;
			timeLeftForAutoRespawn = 0;
			PlayerState playerState = LatentPlayers.Get(medicId);
			Utils.SendNotification(LocalizationController.S(isEMS ? Entries.Death.PARAMEDIC_REVIVED : Entries.Death.DOCTOR_REVIVED, "<C>" + playerState.ColorNameAndId + "</C>"));
			if (isEMS)
			{
				BaseScript.TriggerServerEvent("gtacnr:police:cuffMeIfWantedAndCopsNearby", new object[0]);
			}
			await Utils.FadeIn();
			BaseScript.TriggerEvent("gtacnr:emsReviveCompleted", new object[2]
			{
				medicId > 0,
				isEMS
			});
			BaseScript.TriggerServerEvent("gtacnr:emsReviveCompleted", new object[2]
			{
				medicId > 0,
				isEMS
			});
			IsAlive = true;
		}
	}

	[EventHandler("gtacnr:emsOnScene")]
	private async void EMSOnScene(int emsPlayerId)
	{
		if ((Entity)(object)Game.PlayerPed == (Entity)null || IsAlive == true)
		{
			return;
		}
		PlayerState playerState = LatentPlayers.Get(emsPlayerId);
		if (playerState != null)
		{
			Utils.SendNotification("Paramedic <C>" + playerState.ColorNameAndId + "</C> is on scene!");
			if (!Game.PlayerPed.IsInVehicle())
			{
				Vector3 oldPos = ((Entity)Game.PlayerPed).Position;
				oldPos.Z += 0.01f;
				API.ClearPedTasksImmediately(((PoolObject)Game.PlayerPed).Handle);
				await BaseScript.Delay(50);
				((Entity)Game.PlayerPed).Velocity = Vector3.Zero;
				((Entity)Game.PlayerPed).Position = oldPos;
			}
			if (timeLeftForAutoRespawn < 15)
			{
				timeLeftForAutoRespawn = 15;
			}
		}
	}

	[EventHandler("gtacnr:setIsBeingRevived")]
	private async void OnSetIsBeingRevived(bool isBeingRevived, bool isEMS)
	{
		this.isBeingRevived = isBeingRevived;
		if (isBeingRevived)
		{
			HideRespawnInstructions();
			HideRespawnTimer();
			return;
		}
		HideRespawnInstructions();
		HideRespawnTimer();
		timeLeftForAutoRespawn = 0;
		await Respawn();
		Utils.SendNotification(LocalizationController.S(isEMS ? Entries.Death.PARAMEDIC_FAILED : Entries.Death.DOCTOR_FAILED));
	}

	private void AddRevengeCommandSuggestions()
	{
		Chat.AddSuggestion("/revenge", LocalizationController.S(Entries.Death.SUGG_REVENGE));
	}

	private void CreateRevengeMenus()
	{
		menus["revenge"] = new Menu(LocalizationController.S(Entries.Death.MENU_REVENGE_TITLE), LocalizationController.S(Entries.Death.MENU_REVENGE_SUBTITLE));
		Menu menu = menus["revenge"];
		MenuItem item = (menuItems["revengeTargets"] = new MenuItem(LocalizationController.S(Entries.Death.MENU_REVENGE_TARGETS_TEXT), LocalizationController.S(Entries.Death.MENU_REVENGE_TARGETS_DESCR)));
		menu.AddMenuItem(item);
		Menu menu2 = menus["revenge"];
		item = (menuItems["revengeClaimants"] = new MenuItem(LocalizationController.S(Entries.Death.MENU_REVENGE_CLAIMANTS_TEXT), LocalizationController.S(Entries.Death.MENU_REVENGE_CLAIMANTS_DESCR)));
		menu2.AddMenuItem(item);
		Menu menu3 = menus["revenge"];
		Dictionary<string, MenuItem> dictionary = menuItems;
		MenuCheckboxItem obj = new MenuCheckboxItem(LocalizationController.S(Entries.Death.MENU_REVENGE_TRANSFERS_TEXT), LocalizationController.S(Entries.Death.MENU_REVENGE_TRANSFERS_DESCR))
		{
			PlaySelectSound = false
		};
		item = obj;
		dictionary["toggleRevengeTransfers"] = obj;
		menu3.AddMenuItem(item);
		Menu menu4 = menus["revenge"];
		Dictionary<string, MenuItem> dictionary2 = menuItems;
		MenuCheckboxItem obj2 = new MenuCheckboxItem(LocalizationController.S(Entries.Death.MENU_REVENGE_TARGET_MARKERS), LocalizationController.S(Entries.Death.MENU_REVENGE_TARGET_MARKERS_DESCR))
		{
			Checked = revengeTargetMarkersEnabled
		};
		item = obj2;
		dictionary2["toggleTargetMarkers"] = obj2;
		menu4.AddMenuItem(item);
		Menu menu5 = menus["revenge"];
		Dictionary<string, MenuItem> dictionary3 = menuItems;
		MenuCheckboxItem obj3 = new MenuCheckboxItem(LocalizationController.S(Entries.Death.MENU_REVENGE_CLAIMANT_MARKERS), LocalizationController.S(Entries.Death.MENU_REVENGE_CLAIMANT_MARKERS_DESCR))
		{
			Checked = revengeClaimantMarkersEnabled
		};
		item = obj3;
		dictionary3["toggleClaimantMarkers"] = obj3;
		menu5.AddMenuItem(item);
		MenuController.AddMenu(menus["revenge"]);
		menus["revenge"].OnCheckboxChange += OnMenuCheckboxChange;
		menus["revengeTargets"] = new Menu(LocalizationController.S(Entries.Death.MENU_REVENGE_TITLE), LocalizationController.S(Entries.Death.MENU_REVENGE_TARGETS_TEXT))
		{
			PlaySelectSound = false
		};
		MenuController.AddSubmenu(menus["revenge"], menus["revengeTargets"]);
		MenuController.BindMenuItem(menus["revenge"], menus["revengeTargets"], menuItems["revengeTargets"]);
		menus["revengeTargets"].InstructionalButtons.Clear();
		menus["revengeTargets"].InstructionalButtons.Add((Control)202, LocalizationController.S(Entries.Main.BTN_BACK));
		menus["revengeTargets"].InstructionalButtons.Add((Control)204, "Transfer");
		menus["revengeTargets"].InstructionalButtons.Add((Control)206, "Hit Contract");
		menus["revengeTargets"].ButtonPressHandlers.Add(new Menu.ButtonPressHandler((Control)204, Menu.ControlPressCheckType.JUST_PRESSED, OnTransferRevenge, disableControl: true));
		menus["revengeTargets"].ButtonPressHandlers.Add(new Menu.ButtonPressHandler((Control)206, Menu.ControlPressCheckType.JUST_PRESSED, OnHitRevenge, disableControl: true));
		menus["revengeClaimants"] = new Menu(LocalizationController.S(Entries.Death.MENU_REVENGE_TITLE), LocalizationController.S(Entries.Death.MENU_REVENGE_CLAIMANTS_TEXT))
		{
			PlaySelectSound = false
		};
		MenuController.AddSubmenu(menus["revenge"], menus["revengeClaimants"]);
		MenuController.BindMenuItem(menus["revenge"], menus["revengeClaimants"], menuItems["revengeClaimants"]);
		menus["revengeClaimants"].InstructionalButtons.Clear();
		menus["revengeClaimants"].InstructionalButtons.Add((Control)202, LocalizationController.S(Entries.Main.BTN_BACK));
	}

	private async void OnMenuCheckboxChange(Menu menu, MenuCheckboxItem menuItem, int itemIndex, bool newCheckedState)
	{
		if (menuItem == menuItems["toggleRevengeTransfers"])
		{
			try
			{
				menuItem.Enabled = false;
				if (await TriggerServerEventAsync<bool>("gtacnr:toggleRevengeTransfers", new object[1] { newCheckedState }))
				{
					Utils.PlaySelectSound();
				}
				else
				{
					Utils.PlayErrorSound();
					menuItem.Checked = !newCheckedState;
				}
				return;
			}
			finally
			{
				menuItem.Enabled = true;
			}
		}
		if (menuItem == menuItems["toggleTargetMarkers"])
		{
			revengeTargetMarkersEnabled = newCheckedState;
			Preferences.RevengeTargetMarkers.Set(newCheckedState);
		}
		else if (menuItem == menuItems["toggleClaimantMarkers"])
		{
			revengeClaimantMarkersEnabled = newCheckedState;
			Preferences.RevengeClaimantMarkersEnabled.Set(newCheckedState);
		}
	}

	private async void OnTransferRevenge(Menu menu, Control control)
	{
		if (menu.GetCurrentMenuItem().ItemData is int targetId)
		{
			if (isBusyTransfering)
			{
				Utils.PlayErrorSound();
				return;
			}
			try
			{
				isBusyTransfering = true;
				PlayerState targetData = LatentPlayers.Get(targetId);
				string text = await Utils.GetUserInput("Transfer Revenge Claim", $"Transfer your revenge claim on {targetData} to another player. Please, enter the id or username of the player you want to transfer this revenge claim to:", "", 30);
				if (string.IsNullOrEmpty(text))
				{
					Utils.PlayErrorSound();
					return;
				}
				int.TryParse(text, out var result);
				PlayerState receiverData = null;
				foreach (PlayerState item in LatentPlayers.All)
				{
					if (item.Id == result || item.Name.ToLowerInvariant().Contains(text.ToLowerInvariant()))
					{
						receiverData = item;
						result = receiverData.Id;
						break;
					}
				}
				if (receiverData == null)
				{
					Utils.SendNotification("The player you entered is ~r~invalid~s~.");
					Utils.PlayErrorSound();
					return;
				}
				if (receiverData.JobEnum.IsPublicService())
				{
					Utils.SendNotification("You cannot transfer a revenge claim to a " + Gtacnr.Utils.GetColorTextCode(receiverData.JobEnum, 0) + receiverData.JobDescription + "~s~.");
					Utils.PlayErrorSound();
					return;
				}
				if (receiverData.Id == Game.Player.ServerId)
				{
					Utils.SendNotification("You cannot transfer the revenge claim to ~r~yourself~s~.");
					Utils.PlayErrorSound();
					return;
				}
				if (receiverData.Id == targetId)
				{
					Utils.SendNotification("You cannot transfer the revenge claim to the ~r~target~s~.");
					Utils.PlayErrorSound();
					return;
				}
				int num = await TriggerServerEventAsync<int>("gtacnr:transferRevenge", new object[2] { targetId, result });
				switch (num)
				{
				case 1:
					Utils.PlaySelectSound();
					Utils.DisplayHelpText("You have transferred your revenge claim on " + targetData.ColorNameAndId + " to " + receiverData.ColorNameAndId + ".");
					MenuController.CloseAllMenus();
					break;
				case 4:
					Utils.DisplayHelpText($"~r~{receiverData} is not accepting revenge claim transfers.", playSound: false);
					Utils.PlayErrorSound();
					break;
				default:
					Utils.DisplayErrorMessage(51, num);
					break;
				}
			}
			catch (Exception exception)
			{
				Print(exception);
			}
			finally
			{
				isBusyTransfering = false;
			}
		}
		Utils.PlayErrorSound();
	}

	private async void OnHitRevenge(Menu menu, Control control)
	{
		if (menu.GetCurrentMenuItem().ItemData is int targetId)
		{
			Utils.PlaySelectSound();
			HitmanContractMenuScript.ShowMenu(menu, onSite: false, targetId);
		}
	}

	[EventHandler("gtacnr:updateRevengeInfo")]
	private void OnUpdateRevengeInfo(byte[] RevengeDataBytes)
	{
		CachedRevengeData = RevengeData.Deserialize(RevengeDataBytes);
	}

	private async void RefreshRevengeMenu()
	{
		menus["revengeTargets"].ClearMenuItems();
		menus["revengeClaimants"].ClearMenuItems();
		menus["revengeTargets"].AddLoadingMenuItem();
		menus["revengeClaimants"].AddLoadingMenuItem();
		if (CachedRevengeData == null || Gtacnr.Utils.CheckTimePassed(CachedRevengeData.T, 60000.0))
		{
			byte[] array = await TriggerServerEventAsync<byte[]>("gtacnr:getRevengeInfo", new object[0]);
			if (array != null)
			{
				CachedRevengeData = RevengeData.Deserialize(array);
			}
		}
		Job jobData = Gtacnr.Data.Jobs.GetJobData(Gtacnr.Client.API.Jobs.CachedJob);
		menus["revengeTargets"].ClearMenuItems();
		menus["revengeClaimants"].ClearMenuItems();
		if (CachedRevengeData == null)
		{
			AddNoTargetsMenuItem();
			AddNoClaimantsMenuItem();
			return;
		}
		if (Gtacnr.Client.API.Jobs.CachedJobEnum.IsPublicService())
		{
			menuItems["revengeTargets"].Enabled = false;
			menuItems["revengeTargets"].Description = LocalizationController.S(Entries.Death.MENU_REVENGE_INVALID_JOB, jobData.Name);
		}
		else
		{
			menuItems["revengeTargets"].Enabled = true;
			menuItems["revengeTargets"].Description = LocalizationController.S(Entries.Death.MENU_REVENGE_TARGETS_DESCR);
		}
		if (CachedRevengeData.Targets.Count == 0)
		{
			AddNoTargetsMenuItem();
		}
		else
		{
			foreach (PlayerState item in from i in CachedRevengeData.Targets
				select LatentPlayers.Get(i) into ps
				where ps != null
				orderby ps.Name
				select ps)
			{
				menus["revengeTargets"].AddMenuItem(new MenuItem(item.ColorTextCode + item.Name)
				{
					Label = $"{item.ColorTextCode}{item.Id}",
					ItemData = item.Id
				});
			}
		}
		if (CachedRevengeData.Claimants.Count == 0)
		{
			AddNoClaimantsMenuItem();
			return;
		}
		foreach (PlayerState item2 in from i in CachedRevengeData.Claimants
			select LatentPlayers.Get(i) into ps
			where ps != null
			orderby ps.Name
			select ps)
		{
			menus["revengeClaimants"].AddMenuItem(new MenuItem(item2.ColorTextCode + item2.Name)
			{
				Label = $"{item2.ColorTextCode}{item2.Id}",
				ItemData = item2.Id
			});
		}
		void AddNoClaimantsMenuItem()
		{
			menus["revengeClaimants"].AddMenuItem(new MenuItem(LocalizationController.S(Entries.Death.MENU_REVENGE_NO_CLAIMANTS_TEXT), LocalizationController.S(Entries.Death.MENU_REVENGE_NO_CLAIMANTS_DESCR)));
		}
		void AddNoTargetsMenuItem()
		{
			menus["revengeTargets"].AddMenuItem(new MenuItem(LocalizationController.S(Entries.Death.MENU_REVENGE_NO_TARGETS_TEXT), LocalizationController.S(Entries.Death.MENU_REVENGE_NO_TARGETS_DESCR)));
		}
	}

	public static void OpenRevengeMenu(Menu parent = null)
	{
		MenuController.CloseAllMenus();
		instance.menus["revenge"].ParentMenu = parent;
		instance.menus["revenge"].OpenMenu();
		instance.RefreshRevengeMenu();
	}

	[Command("revenge")]
	private async void RevengeCommand()
	{
		OpenRevengeMenu();
	}
}
