using System;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Gtacnr.Client.Admin;
using Gtacnr.Client.Characters.Lifecycle;
using Gtacnr.Model.Enums;

namespace Gtacnr.Client.Anticheat;

public class AntiHealthLockScript : Script
{
	private DateTime invincibilityDetectionTimeStamp = DateTime.MinValue;

	private int invincibilityDetections;

	public static object HealThreadLock = new object();

	private static bool hasJustHealed = false;

	private int healDetections;

	private static int previousHealth = 0;

	public static object ArmorThreadLock = new object();

	private static bool hasJustUsedArmor = false;

	private int armorDetections;

	private static int previousArmor = 0;

	public static int MaxHealth => (MainScript.HardcoreMode ? 225 : 300) + 100;

	public static int MaxArmor
	{
		get
		{
			if (!MainScript.HardcoreMode)
			{
				return 200;
			}
			return 150;
		}
	}

	public static void JustHealed()
	{
		hasJustHealed = true;
	}

	public static void JustUsedArmor()
	{
		hasJustUsedArmor = true;
	}

	public AntiHealthLockScript()
	{
		foreach (PickupHash value in Enum.GetValues(typeof(PickupHash)))
		{
			API.ToggleUsePickupsForPlayer(Game.Player.Handle, (uint)value, false);
		}
	}

	private bool IsPlayerInSafeState()
	{
		if (!DeathScript.HasSpawnProtection && !NoClipScript.IsNoClipActive && !Utils.IsFrozen && !ModeratorMenuScript.IsOnDuty)
		{
			return !SpawnScript.HasSpawned;
		}
		return true;
	}

	public static void Initialize(int health, int armor)
	{
		previousHealth = health;
		previousArmor = armor;
	}

	[Update]
	private async Coroutine InvincibilityTask()
	{
		await Script.Wait(500);
		if (!IsPlayerInSafeState() && ((Entity)Game.PlayerPed).IsInvincible && Gtacnr.Utils.CheckTimePassed(invincibilityDetectionTimeStamp, 10000.0))
		{
			invincibilityDetectionTimeStamp = DateTime.UtcNow;
			invincibilityDetections++;
			if (invincibilityDetections == 3)
			{
				BaseScript.TriggerServerEvent("gtacnr:ac:banMe", new object[4] { 30, 2, "health lock", "invincibility" });
			}
		}
	}

	[Update]
	private async Coroutine HealArmorTask()
	{
		await Script.Wait(200);
		API.SetPedMaxHealth(((PoolObject)Game.PlayerPed).Handle, MaxHealth);
		API.SetPlayerMaxArmour(Game.Player.Handle, MaxArmor);
		if (IsPlayerInSafeState())
		{
			return;
		}
		lock (HealThreadLock)
		{
			HealDetection();
		}
		lock (ArmorThreadLock)
		{
			ArmorDetection();
		}
	}

	private void HealDetection()
	{
		int health = ((Entity)Game.PlayerPed).Health;
		if (health < 0)
		{
			return;
		}
		if (health > previousHealth + 5)
		{
			if (!hasJustHealed)
			{
				((Entity)Game.PlayerPed).Health = previousHealth;
				BaseScript.TriggerServerEvent("gtacnr:ac:logMePublic", new object[1] { $"\ud83e\ude79 {{0}} might be using health lock - health: {previousHealth} => {health}" });
				healDetections++;
				if (healDetections == 2)
				{
					return;
				}
			}
			hasJustHealed = false;
		}
		previousHealth = health;
	}

	private void ArmorDetection()
	{
		int armor = Game.PlayerPed.Armor;
		if (armor > previousArmor + 5)
		{
			if (!hasJustUsedArmor)
			{
				Game.PlayerPed.Armor = previousArmor;
				BaseScript.TriggerServerEvent("gtacnr:ac:logMePublic", new object[1] { $"\ud83e\ude79 {{0}} might be using health lock - armor: {previousArmor} => {armor}" });
				armorDetections++;
				if (armorDetections == 2)
				{
					return;
				}
			}
			hasJustUsedArmor = false;
		}
		previousArmor = armor;
	}
}
