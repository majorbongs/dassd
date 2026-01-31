using System;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Gtacnr.Client.Anticheat;
using Gtacnr.Client.Jobs.Police.Arrest;
using Gtacnr.Client.Weapons;
using Gtacnr.Data;
using Gtacnr.Localization;
using Gtacnr.Model;

namespace Gtacnr.Client.Items;

public class MedkitScript : Script
{
	private static DateTime usingItemT;

	private DateTime medkitUseTimestamp;

	public static bool IsInMedkitAnimation;

	public static bool IsUsingHealingItem
	{
		get
		{
			return !Gtacnr.Utils.CheckTimePassed(usingItemT, 5000.0);
		}
		set
		{
			if (value)
			{
				usingItemT = DateTime.Now;
			}
			else
			{
				usingItemT = default(DateTime);
			}
		}
	}

	[EventHandler("gtacnr:inventories:usingItem")]
	private void OnUsingItem(string itemId, float amount)
	{
		InventoryItem itemDefinition = Gtacnr.Data.Items.GetItemDefinition(itemId);
		if (itemDefinition == null || !itemId.In("medkit", "bandage"))
		{
			return;
		}
		if (((Entity)Game.PlayerPed).Health == 0)
		{
			API.CancelEvent();
		}
		else if (((Entity)Game.PlayerPed).IsOnFire)
		{
			Utils.SendNotification(LocalizationController.S(Entries.Inventory.INV_CANT_USE_WHEN_ON_FIRE, itemDefinition.Name));
			API.CancelEvent();
		}
		else if (Game.PlayerPed.IsBeingStunned)
		{
			Utils.SendNotification(LocalizationController.S(Entries.Inventory.INV_CANT_USE_WHEN_BEING_STUNNED, itemDefinition.Name));
			API.CancelEvent();
		}
		else if (DrugScript.IsOverdosing)
		{
			Utils.SendNotification(LocalizationController.S(Entries.Inventory.INV_CANT_USE_WHEN_OVERDOSE, itemDefinition.Name));
			API.CancelEvent();
		}
		else if (CuffedScript.IsBeingCuffedOrUncuffed)
		{
			Utils.SendNotification(LocalizationController.S(Entries.Inventory.INV_CANT_USE_WHEN_BEING_CUFFED, itemDefinition.Name));
			API.CancelEvent();
		}
		else if (CuffedScript.IsCuffed)
		{
			Utils.SendNotification(LocalizationController.S(Entries.Inventory.INV_CANT_USE_WHEN_CUFFED, itemDefinition.Name));
			API.CancelEvent();
		}
		else if (itemId == "medkit")
		{
			if (((Entity)Game.PlayerPed).Health >= ((Entity)Game.PlayerPed).MaxHealth)
			{
				API.CancelEvent();
				return;
			}
			if (amount != 1f)
			{
				Print("Amount is not 1.");
				API.CancelEvent();
				return;
			}
			int num = 20 * ((!MainScript.HardcoreMode) ? 1 : 2);
			if (!Gtacnr.Utils.CheckTimePassed(medkitUseTimestamp, num * 1000))
			{
				int num2 = Convert.ToInt32(Math.Round((double)num - (DateTime.UtcNow - medkitUseTimestamp).TotalSeconds));
				Utils.SendNotification(LocalizationController.S(Entries.Inventory.INV_WAIT_COOLDOWN, num2, itemDefinition.Name));
				API.CancelEvent();
			}
			else if (IsUsingHealingItem)
			{
				API.CancelEvent();
			}
			else
			{
				IsUsingHealingItem = true;
			}
		}
		else if (itemId == "bandage")
		{
			if ((float)((Entity)Game.PlayerPed).Health >= (float)((Entity)Game.PlayerPed).MaxHealth * 0.8f)
			{
				Utils.SendNotification(LocalizationController.S(Entries.Inventory.INV_CANT_USE_WHEN_HEALTH_ABOVE_VALUE, itemDefinition.Name, 80));
				API.CancelEvent();
			}
			else if (IsUsingHealingItem)
			{
				API.CancelEvent();
			}
			else
			{
				IsUsingHealingItem = true;
			}
		}
	}

	[EventHandler("gtacnr:useBandage")]
	private void UseBandage(float amount)
	{
		string invokingResource = API.GetInvokingResource();
		if (invokingResource != null)
		{
			BaseScript.TriggerServerEvent("gtacnr:ac:banMe", new object[4]
			{
				30,
				2,
				"triggering",
				"Client event name: gtacnr:useBandage. Resource name: " + invokingResource
			});
		}
		IsUsingHealingItem = false;
		int entityMaxHealth = API.GetEntityMaxHealth(API.PlayerPedId());
		int entityHealth = API.GetEntityHealth(API.PlayerPedId());
		if (entityHealth != 0)
		{
			int num = Math.Min((int)((float)entityHealth + (float)entityMaxHealth * 0.2f * amount), (int)((float)entityMaxHealth * 0.8f));
			lock (AntiHealthLockScript.HealThreadLock)
			{
				AntiHealthLockScript.JustHealed();
				API.SetEntityHealth(API.PlayerPedId(), num);
			}
			Utils.ClearPedDamage(Game.PlayerPed);
			if (amount == 1f)
			{
				Utils.SendNotification(LocalizationController.S(Entries.Specialitems.SPECIALITEM_BANDAGE));
			}
			else if (amount < 1f)
			{
				Utils.SendNotification(LocalizationController.S(Entries.Specialitems.SPECIALITEM_BANDAGE_PIECE));
			}
			else
			{
				Utils.SendNotification($"You've used {amount:0.##} ~p~bandages~s~.");
			}
			Animate(1000);
		}
	}

	[EventHandler("gtacnr:useMedkit")]
	private void UseMedkit()
	{
		string invokingResource = API.GetInvokingResource();
		if (invokingResource != null)
		{
			BaseScript.TriggerServerEvent("gtacnr:ac:banMe", new object[4]
			{
				30,
				2,
				"triggering",
				"Client event name: gtacnr:useMedkit. Resource name: " + invokingResource
			});
		}
		IsUsingHealingItem = false;
		if (API.GetEntityHealth(API.PlayerPedId()) == 0)
		{
			Print("Health is 0.");
			return;
		}
		medkitUseTimestamp = DateTime.UtcNow;
		lock (AntiHealthLockScript.HealThreadLock)
		{
			AntiHealthLockScript.JustHealed();
			API.SetEntityHealth(API.PlayerPedId(), API.GetEntityMaxHealth(API.PlayerPedId()));
		}
		Utils.SendNotification(LocalizationController.S(Entries.Specialitems.SPECIALITEM_MEDKIT));
		Utils.ClearPedDamage(Game.PlayerPed);
		Animate(3000);
	}

	[EventHandler("gtacnr:inventories:itemUseFailed")]
	private void OnItemUseFailed(string itemId, float amount, int errorCode)
	{
		if (itemId.In("medkit", "bandage"))
		{
			IsUsingHealingItem = false;
		}
	}

	[EventHandler("gtacnr:died")]
	private void OnDead(int killerId, int cause)
	{
		medkitUseTimestamp = default(DateTime);
	}

	private async void Animate(int duration)
	{
		try
		{
			IsInMedkitAnimation = true;
			WeaponBehaviorScript.BlockWeaponSwitchingById("medkit");
			string animDict = "anim@amb@board_room@supervising@";
			string animName = "dissaproval_01_lo_amy_skater_01";
			Game.PlayerPed.Task.PlayAnimation(animDict, animName, 4f, duration, (AnimationFlags)51);
			await BaseScript.Delay(duration);
			Game.PlayerPed.Task.ClearAnimation(animDict, animName);
			WeaponBehaviorScript.UnblockWeaponSwitchingById("medkit");
			Weapon weaponBeforeSwitchingBlocked = WeaponBehaviorScript.WeaponBeforeSwitchingBlocked;
			if (weaponBeforeSwitchingBlocked != null && WeaponBehaviorScript.CanSwitchWeapons)
			{
				Game.PlayerPed.Weapons.Select(weaponBeforeSwitchingBlocked);
			}
		}
		finally
		{
			IsInMedkitAnimation = false;
		}
	}
}
