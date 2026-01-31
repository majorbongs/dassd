using System;
using System.Collections.Generic;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Gtacnr.Client.Characters.Lifecycle;
using Gtacnr.Data;
using Gtacnr.Model;

namespace Gtacnr.Client.Anticheat;

public class AntiDamageModifierScript : Script
{
	private DateTime detectTimeStamp = DateTime.MinValue;

	private int playerDmgModDetections;

	private bool compModsDetected;

	private bool weaponModsDetected;

	private static float meleeWeaponDamageModifier = 1f;

	private static float meleeWeaponDefenseModifier = 1f;

	private static float weaponDefenseModifier = 1f;

	public static float MeleeWeaponDamageModifier
	{
		get
		{
			return meleeWeaponDamageModifier;
		}
		set
		{
			meleeWeaponDamageModifier = value;
			API.SetPlayerMeleeWeaponDamageModifier(API.PlayerId(), value);
		}
	}

	public static float MeleeWeaponDefenseModifier
	{
		get
		{
			return meleeWeaponDefenseModifier;
		}
		set
		{
			meleeWeaponDefenseModifier = value;
			API.SetPlayerMeleeWeaponDefenseModifier(API.PlayerId(), value);
		}
	}

	public static float WeaponDefenseModifier
	{
		get
		{
			return weaponDefenseModifier;
		}
		set
		{
			weaponDefenseModifier = value;
			API.SetPlayerWeaponDefenseModifier(API.PlayerId(), value);
		}
	}

	protected override void OnStarted()
	{
		MeleeWeaponDamageModifier = 1f;
		WeaponDefenseModifier = 1f;
		MeleeWeaponDefenseModifier = 1f;
	}

	[Update]
	private async Coroutine DetectDmgModTask()
	{
		await Script.Wait(2000);
		if (SpawnScript.HasSpawned && !CheckPlayerModifiers() && !CheckComponentModifiers())
		{
			CheckWeaponModifiers();
		}
	}

	private bool CheckPlayerModifiers()
	{
		float playerWeaponDamageModifier = API.GetPlayerWeaponDamageModifier(API.PlayerId());
		float playerVehicleDamageModifier = API.GetPlayerVehicleDamageModifier(API.PlayerId());
		List<float> list = new List<float> { 0f, 1f, 0.1f };
		if ((!list.Contains(playerWeaponDamageModifier) || !list.Contains(playerVehicleDamageModifier)) && Gtacnr.Utils.CheckTimePassed(detectTimeStamp, 10000.0))
		{
			detectTimeStamp = DateTime.UtcNow;
			playerDmgModDetections++;
			if (playerDmgModDetections == 3)
			{
				BaseScript.TriggerServerEvent("gtacnr:ac:banMe", new object[4]
				{
					30,
					2,
					"damage modifiers",
					$"Gun Attack: {playerWeaponDamageModifier} | Vehicle Attack: {playerVehicleDamageModifier}"
				});
				return true;
			}
		}
		return false;
	}

	private bool CheckComponentModifiers()
	{
		if (compModsDetected)
		{
			return true;
		}
		Dictionary<string, Tuple<float, float, int>> dictionary = new Dictionary<string, Tuple<float, float, int>>();
		foreach (WeaponComponentDefinition allWeaponComponentDefinition in Gtacnr.Data.Items.GetAllWeaponComponentDefinitions())
		{
			int hashKey = API.GetHashKey(allWeaponComponentDefinition.Id);
			float weaponComponentDamageModifier = API.GetWeaponComponentDamageModifier((uint)hashKey);
			float weaponComponentAccuracyModifier = API.GetWeaponComponentAccuracyModifier((uint)hashKey);
			int weaponComponentClipSize = API.GetWeaponComponentClipSize((uint)hashKey);
			if ((double)weaponComponentDamageModifier > 1.05 || (double)weaponComponentAccuracyModifier > 2.5 || weaponComponentClipSize == -1)
			{
				dictionary[allWeaponComponentDefinition.Id] = Tuple.Create(weaponComponentDamageModifier, weaponComponentAccuracyModifier, weaponComponentClipSize);
			}
		}
		if (dictionary.Count > 0)
		{
			compModsDetected = true;
			BaseScript.TriggerServerEvent("gtacnr:ac:banMe", new object[4]
			{
				30,
				2,
				"damage modifiers",
				dictionary.Json()
			});
			return true;
		}
		return false;
	}

	private bool CheckWeaponModifiers()
	{
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Expected I4, but got Unknown
		if (weaponModsDetected)
		{
			return true;
		}
		HashSet<uint> obj = new HashSet<uint>(Gtacnr.Data.Items.GetAllWeaponHashes()) { (uint)(int)Game.PlayerPed.Weapons.Current.Hash };
		Dictionary<uint, float> dictionary = new Dictionary<uint, float>();
		foreach (uint item in obj)
		{
			float weaponDamageModifier = API.GetWeaponDamageModifier(item);
			if (weaponDamageModifier != 1f && weaponDamageModifier != 0f)
			{
				dictionary[item] = weaponDamageModifier;
			}
		}
		if (dictionary.Count > 0)
		{
			weaponModsDetected = true;
			BaseScript.TriggerServerEvent("gtacnr:ac:banMe", new object[4]
			{
				30,
				2,
				"damage modifiers",
				dictionary.Json()
			});
			return true;
		}
		return false;
	}
}
