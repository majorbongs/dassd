using System;
using System.Collections.Generic;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Gtacnr.Client.Admin;
using Gtacnr.Client.API;
using Gtacnr.Client.API.Scripts;
using Gtacnr.Client.Characters.Lifecycle;
using Gtacnr.Client.Communication;
using Gtacnr.Client.Items;
using Gtacnr.Client.Phone;
using Gtacnr.Client.Vehicles;
using Gtacnr.Data;
using Gtacnr.Model;
using Gtacnr.Model.Enums;
using MenuAPI;

namespace Gtacnr.Client.Weapons;

public class WeaponBehaviorScript : Script
{
	private static readonly int SWITCH_COOLDOWN = 750;

	private static WeaponBehaviorScript instance;

	private DateTime quickDrawWeaponT = DateTime.MinValue;

	private DateTime weaponChangedT = DateTime.MinValue;

	private DateTime ammoChangedT = DateTime.MinValue;

	private DateTime quickSwitchT = DateTime.MinValue;

	private const WeaponHash fistWeapon = (WeaponHash)2725352035u;

	private WeaponHash lastWeapon = (WeaponHash)(-1569615261);

	private bool weaponHudShown;

	private bool ammoHudShown;

	private uint prevWeapon;

	private int prevAmmo;

	private int prevMagazine;

	private bool isAimingWithSniper;

	private static readonly Dictionary<uint, int> lastWeaponMags = new Dictionary<uint, int>();

	private static readonly Dictionary<uint, DateTime> lastWeaponMagsT = new Dictionary<uint, DateTime>();

	private static readonly Dictionary<uint, DateTime> lastWeaponChangeT = new Dictionary<uint, DateTime>();

	private static bool wasReloading = false;

	private Dictionary<uint, DateTime> weaponLastShot = new Dictionary<uint, DateTime>();

	private Dictionary<uint, float> fireRates = new Dictionary<uint, float>();

	private HashSet<int> antiSpamIgnoredGroups = new HashSet<int>
	{
		API.GetHashKey("GROUP_SMG"),
		API.GetHashKey("GROUP_MG"),
		API.GetHashKey("GROUP_RIFLE")
	};

	private HashSet<int> antiSpamIgnoredWeapons = new HashSet<int>
	{
		API.GetHashKey("weapon_pistol"),
		API.GetHashKey("weapon_pistol_mk2"),
		API.GetHashKey("weapon_combatpistol"),
		API.GetHashKey("weapon_appistol"),
		API.GetHashKey("weapon_snspistol"),
		API.GetHashKey("weapon_snspistol_mk2"),
		API.GetHashKey("weapon_vintagepistol"),
		API.GetHashKey("weapon_doubleaction"),
		API.GetHashKey("weapon_ceramicpistol")
	};

	private HashSet<int> antiSpamUnignoredWeapons = new HashSet<int> { API.GetHashKey("weapon_carbinerifle") };

	private Dictionary<int, int> burstFireWeapons = new Dictionary<int, int> { 
	{
		API.GetHashKey("weapon_appistol"),
		3
	} };

	private static readonly List<string> weaponSwitchBlockers = new List<string>();

	private static bool canSwitchWeapons = true;

	public static CameraCycleMode CameraCycleMode { get; set; }

	public static DateTime LastGunShotTime { get; private set; } = DateTime.MinValue;

	public static Weapon WeaponBeforeSwitchingBlocked { get; private set; }

	public static bool CanSwitchWeapons
	{
		get
		{
			if (canSwitchWeapons)
			{
				return weaponSwitchBlockers.Count == 0;
			}
			return false;
		}
		private set
		{
			if (!value && value != canSwitchWeapons)
			{
				WeaponBeforeSwitchingBlocked = Game.PlayerPed.Weapons.Current;
				Game.PlayerPed.Weapons.Select((WeaponHash)(-1569615261));
			}
			canSwitchWeapons = value;
			Game.PlayerPed.CanSwitchWeapons = value;
		}
	}

	public WeaponBehaviorScript()
	{
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		instance = this;
		VehicleEvents.EnteredVehicle += OnEnteredOrLeftVehicle;
		VehicleEvents.LeftVehicle += OnEnteredOrLeftVehicle;
	}

	private void OnEnteredOrLeftVehicle(object sender, VehicleEventArgs e)
	{
		prevWeapon = 0u;
		lastWeaponMags.Clear();
		lastWeaponMagsT.Clear();
	}

	[EventHandler("gtacnr:respawned")]
	private void OnRespawned()
	{
		prevWeapon = 0u;
		lastWeaponMags.Clear();
		lastWeaponMagsT.Clear();
	}

	public static void BlockWeaponSwitchingById(string id)
	{
		weaponSwitchBlockers.Add(id);
		CanSwitchWeapons = false;
	}

	public static void BlockWeaponSwitchingByDistinctId(string id)
	{
		if (!weaponSwitchBlockers.Contains(id))
		{
			weaponSwitchBlockers.Add(id);
			CanSwitchWeapons = false;
		}
	}

	public static void UnblockWeaponSwitchingById(string id)
	{
		weaponSwitchBlockers.Remove(id);
		if (weaponSwitchBlockers.Count == 0)
		{
			CanSwitchWeapons = true;
		}
	}

	protected override void OnStarted()
	{
		API.SetWeaponsNoAutoswap(true);
		API.SetFlashLightKeepOnWhileMoving(Preferences.FlashlightModeEnabled.Get());
		Chat.AddSuggestion("/equip", "Instantly equips a weapon by its id.", new ChatParamSuggestion("weaponId", "The id of the weapon you want to equip."));
		CameraCycleMode = Preferences.CameraCycleMode.Get();
	}

	private void ShowWeaponHud()
	{
		if (!weaponHudShown && !API.IsPedInAnyVehicle(API.PlayerPedId(), false))
		{
			weaponHudShown = true;
			BaseScript.TriggerEvent("gtacnr:hud:toggleWeapon", new object[1] { true });
		}
	}

	private void HideWeaponHud()
	{
		if (weaponHudShown)
		{
			weaponHudShown = false;
			BaseScript.TriggerEvent("gtacnr:hud:toggleWeapon", new object[1] { false });
		}
	}

	private void ShowAmmoHud()
	{
		if (!ammoHudShown && API.GetWeaponDamageType(prevWeapon) != 2 && prevWeapon != (uint)API.GetHashKey("weapon_stungun"))
		{
			ammoHudShown = true;
			BaseScript.TriggerEvent("gtacnr:hud:toggleAmmo", new object[1] { true });
		}
	}

	private void HideAmmoHud(bool noFadeOut = false)
	{
		if (ammoHudShown)
		{
			ammoHudShown = false;
			BaseScript.TriggerEvent("gtacnr:hud:toggleAmmo", new object[2] { false, noFadeOut });
		}
	}

	private void SetWeaponInHud(WeaponHash weaponHash)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0002: Expected I4, but got Unknown
		string text = ((uint)(int)weaponHash).ToString("X");
		BaseScript.TriggerEvent("gtacnr:hud:setWeapon", new object[1] { text });
	}

	private void SetAmmoInHud(int ammo, int magazine)
	{
		BaseScript.TriggerEvent("gtacnr:hud:setAmmo", new object[1] { ammo });
		BaseScript.TriggerEvent("gtacnr:hud:setMagazine", new object[1] { magazine });
	}

	[Update]
	private async Coroutine WeaponSwayTask()
	{
		await Script.Wait(50);
		if (Game.PlayerPed.IsAiming)
		{
			WeaponDefinition weaponDefinitionByHash = Gtacnr.Data.Items.GetWeaponDefinitionByHash((uint)(int)Game.PlayerPed.Weapons.Current.Hash);
			if (weaponDefinitionByHash != null && weaponDefinitionByHash.Sway > 0f && !isAimingWithSniper)
			{
				isAimingWithSniper = true;
				API.ShakeGameplayCam("DRUNK_SHAKE", weaponDefinitionByHash.Sway);
			}
		}
		else if (isAimingWithSniper)
		{
			isAimingWithSniper = false;
			API.ShakeGameplayCam("DRUNK_SHAKE", 0f);
		}
	}

	private void AddBreathIntructions()
	{
	}

	private void RemoveBreathIntructions()
	{
	}

	[Update]
	private async Coroutine CheckTask()
	{
		await Script.Wait(5);
		if (DeathScript.IsAlive != true)
		{
			return;
		}
		bool isReloading;
		uint weapon;
		int ammo;
		int magazine;
		while (true)
		{
			isReloading = API.IsPedReloading(API.PlayerPedId());
			weapon = 0u;
			API.GetCurrentPedWeapon(API.PlayerPedId(), ref weapon, true);
			ammo = API.GetAmmoInPedWeapon(API.PlayerPedId(), weapon);
			magazine = 0;
			API.GetAmmoInClip(API.PlayerPedId(), weapon, ref magazine);
			if (weapon == prevWeapon || MenuController.IsAnyMenuOpen() || PhoneMenuScript.IsCalling || PhoneScript.IsPhoneOpen)
			{
				break;
			}
			lastWeaponChangeT[weapon] = DateTime.UtcNow;
			if (prevWeapon != 0 && Gtacnr.Utils.CheckTimePassed(lastWeaponChangeT[weapon], 1000.0))
			{
				lastWeaponMags[prevWeapon] = prevMagazine;
				lastWeaponMagsT[prevWeapon] = DateTime.UtcNow;
			}
			if (lastWeaponMags.TryGetValue(weapon, out var oldMag) && ammo != 0 && lastWeaponMagsT.TryGetValue(weapon, out var value) && !Gtacnr.Utils.CheckTimePassed(value, 8000.0))
			{
				WeaponHash weaponToReload = (WeaponHash)weapon;
				while (magazine == 0 && weapon == (uint)(int)weaponToReload && (!API.IsPedReloading(API.PlayerPedId()) || wasReloading))
				{
					API.GetAmmoInClip(API.PlayerPedId(), weapon, ref magazine);
					API.GetCurrentPedWeapon(API.PlayerPedId(), ref weapon, true);
					await Script.Yield();
				}
				if (DeathScript.IsAlive != true)
				{
					return;
				}
				if (weapon != (uint)(int)weaponToReload)
				{
					prevWeapon = weapon;
					SetWeaponInHud((WeaponHash)weapon);
					weaponChangedT = DateTime.UtcNow;
					wasReloading = isReloading;
					continue;
				}
				if (magazine > oldMag)
				{
					int num = magazine - oldMag;
					if (num > 0)
					{
						API.SetAmmoInClip(API.PlayerPedId(), weapon, oldMag);
						API.AddAmmoToPed(API.PlayerPedId(), (uint)(int)weaponToReload, num);
						API.GetAmmoInClip(API.PlayerPedId(), weapon, ref magazine);
						num = oldMag - magazine;
						API.AddAmmoToPed(API.PlayerPedId(), (uint)(int)weaponToReload, num);
					}
				}
			}
			prevWeapon = weapon;
			SetWeaponInHud((WeaponHash)weapon);
			weaponChangedT = DateTime.UtcNow;
			break;
		}
		API.GetAmmoInClip(API.PlayerPedId(), weapon, ref magazine);
		if (ammo != prevAmmo || magazine != prevMagazine)
		{
			prevAmmo = ammo;
			prevMagazine = magazine;
			SetAmmoInHud(ammo - magazine, magazine);
			ammoChangedT = DateTime.UtcNow;
			if (API.GetWeaponDamageType(weapon) == 2 || weapon == (uint)API.GetHashKey("weapon_stungun"))
			{
				HideAmmoHud(noFadeOut: true);
			}
		}
		API.SetPlayerVehicleDefenseModifier(API.PlayerId(), 0.3f);
		wasReloading = isReloading;
	}

	[Update]
	private async Coroutine UpdateTask()
	{
		Ped playerPed = Game.PlayerPed;
		int playerPedHandle = ((PoolObject)playerPed).Handle;
		if (!Gtacnr.Utils.CheckTimePassed(weaponChangedT, 4250.0))
		{
			ShowWeaponHud();
		}
		else
		{
			HideWeaponHud();
		}
		if (!Gtacnr.Utils.CheckTimePassed(ammoChangedT, 4250.0))
		{
			ShowAmmoHud();
		}
		else
		{
			HideAmmoHud();
		}
		uint weapon = 0u;
		API.GetCurrentPedWeapon(API.PlayerPedId(), ref weapon, true);
		if (((Entity)Game.PlayerPed).IsOnFire)
		{
			DisableShootingControls(disableVehicle: true, disableMelee: true);
		}
		if (burstFireWeapons.ContainsKey((int)weapon) && Game.IsControlJustPressed(2, (Control)24))
		{
			int burstAmmo = burstFireWeapons[(int)weapon];
			int curAmmo = API.GetAmmoInPedWeapon(playerPedHandle, weapon);
			while (Game.IsControlPressed(2, (Control)24))
			{
				await Script.Wait(50);
				int num = curAmmo - burstAmmo;
				if (num < 0)
				{
					num = 0;
				}
				if (API.GetAmmoInPedWeapon(playerPedHandle, weapon) <= num)
				{
					break;
				}
			}
			DateTime lastPressT = DateTime.UtcNow;
			while (Game.IsControlPressed(2, (Control)24) || !Gtacnr.Utils.CheckTimePassed(lastPressT, 90.0))
			{
				API.DisablePlayerFiring(playerPedHandle, true);
				await Script.Yield();
			}
		}
		if (playerPed.IsInVehicle() || ((Entity)playerPed).IsDead)
		{
			ToggleWeaponControls(toggle: true);
			return;
		}
		bool isTaskActiveEx = Utils.GetIsTaskActiveEx(playerPedHandle, TaskTypeIndex.CTaskCombatRoll);
		if (isTaskActiveEx)
		{
			Game.DisableControlThisFrame(2, (Control)0);
		}
		if (CameraCycleMode != CameraCycleMode.Normal && !isTaskActiveEx)
		{
			Game.DisableControlThisFrame(2, (Control)0);
			if (Game.IsDisabledControlJustPressed(2, (Control)0))
			{
				WaitForRelease();
			}
		}
		if (weaponLastShot.ContainsKey(weapon))
		{
			if (!fireRates.ContainsKey(weapon))
			{
				fireRates[weapon] = BitConverter.ToSingle(BitConverter.GetBytes(API.GetWeaponTimeBetweenShots(weapon)), 0);
			}
			float num2 = fireRates[weapon];
			if ((float)(DateTime.UtcNow - weaponLastShot[weapon]).TotalSeconds < num2)
			{
				DisableShootingControls();
			}
		}
		int weapontypeGroup = API.GetWeapontypeGroup(weapon);
		if (API.IsPedShooting(API.PlayerPedId()))
		{
			LastGunShotTime = DateTime.UtcNow;
			if ((!antiSpamIgnoredGroups.Contains(weapontypeGroup) || antiSpamUnignoredWeapons.Contains((int)weapon)) && !antiSpamIgnoredWeapons.Contains((int)weapon))
			{
				weaponLastShot[weapon] = DateTime.UtcNow;
			}
		}
		if (!Gtacnr.Utils.CheckTimePassed(quickSwitchT, SWITCH_COOLDOWN))
		{
			DisableAimingControls();
			DisableShootingControls();
		}
		if (API.IsPauseMenuActive() || MenuController.IsAnyMenuOpen() || ChatScript.IsChatInputOpen)
		{
			return;
		}
		if (API.IsDisabledControlJustPressed(2, 37) || API.IsControlJustPressed(2, 37))
		{
			quickDrawWeaponT = DateTime.UtcNow;
		}
		else if ((API.IsDisabledControlJustReleased(2, 37) || API.IsControlJustReleased(2, 37)) && !Gtacnr.Utils.CheckTimePassed(quickDrawWeaponT, 200.0))
		{
			if (MenuController.IsAnyMenuOpen() || !CanSwitchWeapons)
			{
				return;
			}
			WeaponHash hash = playerPed.Weapons.Current.Hash;
			if ((int)hash == -1569615261)
			{
				if ((int)lastWeapon == -1569615261)
				{
					List<WeaponHash> allPlayerWeapons = GetAllPlayerWeapons();
					if (allPlayerWeapons.Count < 2)
					{
						return;
					}
					lastWeapon = allPlayerWeapons[1];
				}
				API.SetCurrentPedWeapon(playerPedHandle, (uint)(int)lastWeapon, true);
			}
			else
			{
				lastWeapon = hash;
				API.SetCurrentPedWeapon(playerPedHandle, 2725352035u, true);
			}
			return;
		}
		if (BinocularsScript.BinocularsViewTaskAttached || NoClipScript.IsNoClipActive || MenuController.IsAnyMenuOpen() || !CanSwitchWeapons || Game.IsControlPressed(2, (Control)24) || Game.IsControlPressed(2, (Control)20))
		{
			return;
		}
		if (API.IsDisabledControlPressed(2, 37) || API.IsControlPressed(2, 37))
		{
			ToggleWeaponControls(toggle: true);
			return;
		}
		ToggleWeaponControls(toggle: false);
		if (!API.IsControlPressed(2, 25))
		{
			if (API.IsDisabledControlJustPressed(2, 16))
			{
				NextWeapon();
			}
			else if (API.IsDisabledControlJustPressed(2, 17))
			{
				PreviousWeapon();
			}
		}
		static void DisableAimingControls()
		{
			API.DisableControlAction(2, 25, true);
			API.DisableControlAction(2, 50, true);
			API.DisableControlAction(2, 68, true);
			API.DisableControlAction(2, 91, true);
		}
		static void DisableShootingControls(bool disableVehicle = false, bool disableMelee = false)
		{
			API.DisableControlAction(2, 24, true);
			API.DisableControlAction(2, 257, true);
			if (disableVehicle)
			{
				API.DisableControlAction(2, 69, true);
				API.DisableControlAction(2, 70, true);
			}
			if (disableMelee)
			{
				API.DisableControlAction(2, 263, true);
				API.DisableControlAction(2, 264, true);
			}
		}
		static async void WaitForRelease()
		{
			DateTime t = DateTime.UtcNow;
			while (Game.IsDisabledControlPressed(2, (Control)0))
			{
				await BaseScript.Delay(0);
			}
			if (!Gtacnr.Utils.CheckTimePassed(t, KeysScript.HOLD_TIME))
			{
				int followPedCamViewMode = API.GetFollowPedCamViewMode();
				int num3 = -1;
				switch (CameraCycleMode)
				{
				case CameraCycleMode.Far:
					num3 = ((followPedCamViewMode == 4) ? 2 : 4);
					break;
				case CameraCycleMode.Medium:
					num3 = ((followPedCamViewMode == 4) ? 1 : 4);
					break;
				case CameraCycleMode.Near:
					num3 = ((followPedCamViewMode != 4) ? 4 : 0);
					break;
				case CameraCycleMode.NoFP:
					API.SetFollowPedCamViewMode(4);
					num3 = MathUtil.Wrap(followPedCamViewMode + 1, 0, 2);
					break;
				}
				if (num3 != -1)
				{
					API.SetFollowPedCamViewMode(num3);
				}
			}
		}
	}

	private void ToggleWeaponControls(bool toggle)
	{
		int[] array = new int[7] { 12, 13, 14, 15, 16, 17, 37 };
		foreach (int num in array)
		{
			if (toggle)
			{
				API.EnableControlAction(2, num, true);
			}
			else
			{
				API.DisableControlAction(2, num, true);
			}
		}
	}

	private void SwitchWeapon(int count)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		Ped playerPed = Game.PlayerPed;
		List<WeaponHash> list = new List<WeaponHash>();
		for (int i = 0; i < 7; i++)
		{
			WeaponHash val = (WeaponHash)API.HudWeaponWheelGetSlotHash(i);
			if ((int)val != 0)
			{
				if (i == 4)
				{
					list.Insert(0, val);
				}
				else
				{
					list.Add(val);
				}
			}
		}
		if (list.Count > 1)
		{
			WeaponHash hash = playerPed.Weapons.Current.Hash;
			int num = list.IndexOf(hash) + count;
			if (num <= list.Count - 1 && num >= 0)
			{
				WeaponHash val2 = list[num];
				Game.PlayerPed.Weapons.Select(val2, Game.PlayerPed.IsReloading);
				Utils.PlayNavSound();
				quickSwitchT = DateTime.UtcNow;
			}
		}
	}

	private void NextWeapon()
	{
		SwitchWeapon(1);
	}

	private void PreviousWeapon()
	{
		SwitchWeapon(-1);
	}

	public static void QuickSwitchToWeapon(WeaponHash weaponHash, bool playSound = true)
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		if (CanSwitchWeapons)
		{
			Game.PlayerPed.Weapons.Select(weaponHash, Game.PlayerPed.IsReloading);
			instance.quickSwitchT = DateTime.UtcNow;
			if (playSound)
			{
				Utils.PlayNavSound();
			}
		}
	}

	private List<WeaponHash> GetAllPlayerWeapons()
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		Ped playerPed = Game.PlayerPed;
		List<WeaponHash> list = new List<WeaponHash>();
		WeaponHash[] weaponHashes = Utils.WeaponHashes;
		foreach (WeaponHash val in weaponHashes)
		{
			if (playerPed.Weapons.HasWeapon(val))
			{
				list.Add(val);
			}
		}
		return list;
	}

	[Command("equip")]
	private void EquipCommand(string[] args)
	{
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		if (args.Length == 0)
		{
			Chat.AddMessage(Gtacnr.Utils.Colors.Error, "Usage: /equip (weapon id).");
			return;
		}
		string text = args[0].ToLowerInvariant();
		if (!Gtacnr.Data.Items.IsWeaponDefined(text))
		{
			Chat.AddMessage(Gtacnr.Utils.Colors.Error, "Invalid weapon id (" + text + ").");
			return;
		}
		WeaponHash val = (WeaponHash)API.GetHashKey(text);
		if (Game.PlayerPed.Weapons.Current.Hash != val)
		{
			QuickSwitchToWeapon(val);
		}
		else
		{
			QuickSwitchToWeapon((WeaponHash)API.GetHashKey("weapon_unarmed"));
		}
	}
}
