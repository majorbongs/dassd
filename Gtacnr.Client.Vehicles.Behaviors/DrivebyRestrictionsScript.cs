using System;
using System.Collections.Generic;
using System.Linq;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Gtacnr.Client.Inventory;
using Gtacnr.Data;
using Gtacnr.Model;

namespace Gtacnr.Client.Vehicles.Behaviors;

public class DrivebyRestrictionsScript : Script
{
	private HashSet<VehicleHash> alwaysDisabled;

	private HashSet<VehicleHash> shootingBehindDisabled;

	private HashSet<VehicleHash> alwaysAllow;

	private HashSet<VehicleClass> allowedClasses;

	private HashSet<WeaponHash> allowedWeapons;

	private bool isDrivebyPartiallyDisabled;

	private bool isDrivebyDisabled;

	private static readonly Dictionary<uint, float> defaultAccuracySpreads = new Dictionary<uint, float>();

	private bool applyingSway;

	protected override void OnStarted()
	{
		Dictionary<string, List<string>> dictionary = Gtacnr.Utils.LoadJson<Dictionary<string, List<string>>>("data/vehicles/driveByRestrictions.json");
		alwaysDisabled = new HashSet<VehicleHash>(dictionary["AlwaysDisabled"].Select((string i) => (VehicleHash)API.GetHashKey(i)));
		shootingBehindDisabled = new HashSet<VehicleHash>(dictionary["ShootingBehindDisabled"].Select((string i) => (VehicleHash)API.GetHashKey(i)));
		alwaysAllow = new HashSet<VehicleHash>(dictionary["AlwaysAllow"].Select((string i) => (VehicleHash)API.GetHashKey(i)));
		allowedClasses = new HashSet<VehicleClass>(dictionary["AllowedClasses"].Select((string i) => (VehicleClass)int.Parse(i)));
		allowedWeapons = new HashSet<WeaponHash>(dictionary["AllowedWeapons"].Select((string i) => (WeaponHash)API.GetHashKey(i)));
		EnableAllWeapons();
		ArmoryScript.LoadoutChanged += OnLoadoutChanged;
		ArmoryScript.WeaponEquipped += OnLoadoutChanged;
		foreach (WeaponDefinition allWeaponDefinition in Gtacnr.Data.Items.GetAllWeaponDefinitions())
		{
			uint hashKey = (uint)API.GetHashKey(allWeaponDefinition.Id);
			float weaponAccuracySpread = API.GetWeaponAccuracySpread(hashKey);
			defaultAccuracySpreads[hashKey] = weaponAccuracySpread;
		}
		VehicleEvents.EnteredVehicle += OnEnteredVehicle;
		VehicleEvents.LeftVehicle += OnLeftVehicle;
	}

	private async void OnEnteredVehicle(object sender, VehicleEventArgs e)
	{
		foreach (KeyValuePair<uint, float> defaultAccuracySpread in defaultAccuracySpreads)
		{
			API.SetWeaponAccuracySpread(defaultAccuracySpread.Key, 10f);
		}
		WeaponHash previousWeapon = Game.PlayerPed.Weapons.Current.Hash;
		int previousAmmo = Game.PlayerPed.Weapons.Current.Ammo;
		while ((Entity)(object)Game.PlayerPed.CurrentVehicle != (Entity)null)
		{
			WeaponHash hash = Game.PlayerPed.Weapons.Current.Hash;
			int ammo = Game.PlayerPed.Weapons.Current.Ammo;
			if (hash == previousWeapon && ammo < previousAmmo)
			{
				API.GetGameplayCamRelativePitch();
				API.ShakeGameplayCam("JOLT_SHAKE", 0.5f);
			}
			previousWeapon = hash;
			previousAmmo = ammo;
			await BaseScript.Delay(10);
		}
	}

	private void OnLeftVehicle(object sender, VehicleEventArgs e)
	{
		foreach (KeyValuePair<uint, float> defaultAccuracySpread in defaultAccuracySpreads)
		{
			API.SetWeaponAccuracySpread(defaultAccuracySpread.Key, defaultAccuracySpread.Value);
		}
	}

	private void OnLoadoutChanged(object sender, EventArgs e)
	{
		if (isDrivebyPartiallyDisabled)
		{
			isDrivebyPartiallyDisabled = false;
		}
	}

	[Update]
	private async Coroutine CheckTask()
	{
		await Script.Wait(200);
		bool flag = isDrivebyPartiallyDisabled;
		isDrivebyPartiallyDisabled = (Entity)(object)Game.PlayerPed != (Entity)null && (Entity)(object)Game.PlayerPed.CurrentVehicle != (Entity)null && (Entity)(object)Game.PlayerPed.CurrentVehicle.Driver == (Entity)(object)Game.PlayerPed && !allowedClasses.Contains(Game.PlayerPed.CurrentVehicle.ClassType) && !alwaysAllow.Contains(Model.op_Implicit(((Entity)Game.PlayerPed.CurrentVehicle).Model));
		if (flag && !isDrivebyPartiallyDisabled)
		{
			EnableAllWeapons();
		}
		else if (!flag && isDrivebyPartiallyDisabled)
		{
			DisableNonAllowedDrivebyWeapons();
		}
		isDrivebyDisabled = false;
		Ped playerPed = Game.PlayerPed;
		if ((Entity)(object)((playerPed != null) ? playerPed.CurrentVehicle : null) != (Entity)null)
		{
			Model model = ((Entity)Game.PlayerPed.CurrentVehicle).Model;
			if (alwaysDisabled.Contains(Model.op_Implicit(model)))
			{
				isDrivebyDisabled = true;
			}
			else if (shootingBehindDisabled.Contains(Model.op_Implicit(model)))
			{
				float gameplayCamRelativeHeading = API.GetGameplayCamRelativeHeading();
				isDrivebyDisabled = gameplayCamRelativeHeading > 110f || gameplayCamRelativeHeading < -110f;
			}
		}
	}

	[Update]
	private async Coroutine DisableTask()
	{
		if (isDrivebyDisabled)
		{
			Game.DisableControlThisFrame(2, (Control)25);
			Game.DisableControlThisFrame(2, (Control)68);
			Game.DisableControlThisFrame(2, (Control)91);
			Game.DisableControlThisFrame(2, (Control)24);
			Game.DisableControlThisFrame(2, (Control)257);
			Game.DisableControlThisFrame(2, (Control)69);
			Game.DisableControlThisFrame(2, (Control)70);
			Game.DisableControlThisFrame(2, (Control)92);
			Game.DisableControlThisFrame(2, (Control)101);
		}
	}

	private void EnableAllWeapons()
	{
		foreach (WeaponDefinition allWeaponDefinition in Gtacnr.Data.Items.GetAllWeaponDefinitions())
		{
			if (!(allWeaponDefinition.Id == "weapon_stungun"))
			{
				int hashKey = API.GetHashKey(allWeaponDefinition.Id);
				API.SetCanPedEquipWeapon(((PoolObject)Game.PlayerPed).Handle, (uint)hashKey, true);
			}
		}
	}

	private void DisableNonAllowedDrivebyWeapons()
	{
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Invalid comparison between I4 and Unknown
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Expected I4, but got Unknown
		uint num = 0u;
		API.GetCurrentPedWeapon(((PoolObject)Game.PlayerPed).Handle, ref num, true);
		foreach (WeaponDefinition allWeaponDefinition in Gtacnr.Data.Items.GetAllWeaponDefinitions())
		{
			if (allWeaponDefinition.Id == "weapon_stungun")
			{
				continue;
			}
			WeaponHash val = (WeaponHash)API.GetHashKey(allWeaponDefinition.Id);
			if (!allowedWeapons.Contains(val))
			{
				if (num == (uint)(int)val)
				{
					Game.PlayerPed.Weapons.Select((WeaponHash)(-1569615261), true);
				}
				API.SetCanPedEquipWeapon(((PoolObject)Game.PlayerPed).Handle, (uint)(int)val, false);
			}
		}
	}
}
