using System;
using System.Collections.Generic;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Gtacnr.Client.Anticheat;
using Gtacnr.Client.Jobs.Police.Arrest;
using Gtacnr.Client.Weapons;
using Gtacnr.Data;
using Gtacnr.Localization;
using Gtacnr.Model;

namespace Gtacnr.Client.Items;

public class BodyArmorScript : Script
{
	private Dictionary<string, DateTime> armorUseTimestamp = new Dictionary<string, DateTime>();

	public static bool IsInArmorAnimation;

	public static bool IsUsingHealingItem
	{
		get
		{
			return MedkitScript.IsUsingHealingItem;
		}
		set
		{
			MedkitScript.IsUsingHealingItem = value;
		}
	}

	[EventHandler("gtacnr:inventories:usingItem")]
	private void OnUsingItem(string itemId, float amount)
	{
		InventoryItem itemDefinition = Gtacnr.Data.Items.GetItemDefinition(itemId);
		if (itemDefinition == null || !itemDefinition.ExtraData.ContainsKey("IsArmor") || !(bool)itemDefinition.ExtraData["IsArmor"])
		{
			return;
		}
		if (Game.PlayerPed.Armor >= 200)
		{
			Print("Armor is max.");
			API.CancelEvent();
			return;
		}
		if (((Entity)Game.PlayerPed).Health == 0)
		{
			Print("Health is 0.");
			API.CancelEvent();
			return;
		}
		if (amount != 1f)
		{
			Print("Amount is not 1.");
			API.CancelEvent();
			return;
		}
		if (((Entity)Game.PlayerPed).IsOnFire)
		{
			Utils.SendNotification(LocalizationController.S(Entries.Inventory.INV_CANT_USE_WHEN_ON_FIRE, itemDefinition.Name));
			API.CancelEvent();
			return;
		}
		if (Game.PlayerPed.IsBeingStunned)
		{
			Utils.SendNotification(LocalizationController.S(Entries.Inventory.INV_CANT_USE_WHEN_BEING_STUNNED, itemDefinition.Name));
			API.CancelEvent();
			return;
		}
		if (DrugScript.IsOverdosing)
		{
			Utils.SendNotification(LocalizationController.S(Entries.Inventory.INV_CANT_USE_WHEN_OVERDOSE, itemDefinition.Name));
			API.CancelEvent();
			return;
		}
		if (CuffedScript.IsBeingCuffedOrUncuffed)
		{
			Utils.SendNotification(LocalizationController.S(Entries.Inventory.INV_CANT_USE_WHEN_BEING_CUFFED, itemDefinition.Name));
			API.CancelEvent();
			return;
		}
		if (CuffedScript.IsCuffed)
		{
			Utils.SendNotification(LocalizationController.S(Entries.Inventory.INV_CANT_USE_WHEN_CUFFED, itemDefinition.Name));
			API.CancelEvent();
			return;
		}
		if (!armorUseTimestamp.ContainsKey(itemId))
		{
			armorUseTimestamp[itemId] = default(DateTime);
		}
		int num = Convert.ToInt32(itemDefinition.ExtraData["Cooldown"]) * ((!MainScript.HardcoreMode) ? 1 : 2);
		if (!Gtacnr.Utils.CheckTimePassed(armorUseTimestamp[itemId], num * 1000))
		{
			int num2 = Convert.ToInt32(Math.Round((double)num - (DateTime.UtcNow - armorUseTimestamp[itemId]).TotalSeconds));
			Utils.SendNotification($"You need to ~r~wait ~s~{num2} seconds to use this type of ~p~body armor ~s~again.");
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

	[EventHandler("gtacnr:equipBodyArmor")]
	private async void EquipBodyArmor(string itemId)
	{
		string invokingResource = API.GetInvokingResource();
		if (invokingResource != null)
		{
			BaseScript.TriggerServerEvent("gtacnr:ac:banMe", new object[4]
			{
				30,
				2,
				"triggering",
				"Client event name: gtacnr:equipBodyArmor. Resource name: " + invokingResource
			});
		}
		armorUseTimestamp[itemId] = DateTime.UtcNow;
		IsUsingHealingItem = false;
		InventoryItem itemDefinition = Gtacnr.Data.Items.GetItemDefinition(itemId);
		int refillAmount = Convert.ToInt32(itemDefinition.ExtraData["RefillAmount"]);
		if (MainScript.HardcoreMode)
		{
			refillAmount = ((float)refillAmount * 0.75f).ToInt();
		}
		lock (AntiHealthLockScript.ArmorThreadLock)
		{
			AntiHealthLockScript.JustUsedArmor();
			Ped playerPed = Game.PlayerPed;
			playerPed.Armor += refillAmount;
			int playerMaxArmour = API.GetPlayerMaxArmour(Game.Player.Handle);
			if (Game.PlayerPed.Armor > playerMaxArmour)
			{
				Game.PlayerPed.Armor = playerMaxArmour;
			}
		}
		Animate();
		async void Animate()
		{
			_ = 1;
			try
			{
				IsInArmorAnimation = true;
				WeaponBehaviorScript.BlockWeaponSwitchingById("armor");
				string animDict = "clothingtie";
				string animName = "try_tie_neutral_c";
				int duration = ((refillAmount <= 50) ? 800 : ((refillAmount <= 100) ? 1500 : 2500));
				API.RequestAnimDict(animDict);
				while (!API.HasAnimDictLoaded(animDict))
				{
					await BaseScript.Delay(0);
				}
				Game.PlayerPed.Task.PlayAnimation(animDict, animName, 4f, duration, (AnimationFlags)51);
				await BaseScript.Delay(duration);
				Game.PlayerPed.Task.ClearAnimation(animDict, animName);
				WeaponBehaviorScript.UnblockWeaponSwitchingById("armor");
				Weapon weaponBeforeSwitchingBlocked = WeaponBehaviorScript.WeaponBeforeSwitchingBlocked;
				if (weaponBeforeSwitchingBlocked != null && WeaponBehaviorScript.CanSwitchWeapons)
				{
					Game.PlayerPed.Weapons.Select(weaponBeforeSwitchingBlocked);
				}
			}
			finally
			{
				IsInArmorAnimation = false;
			}
		}
	}

	[EventHandler("gtacnr:inventories:itemUseFailed")]
	private void OnItemUseFailed(string itemId, float amount, int errorCode)
	{
		InventoryItem itemDefinition = Gtacnr.Data.Items.GetItemDefinition(itemId);
		if (itemDefinition.ExtraData.ContainsKey("IsArmor") && (bool)itemDefinition.ExtraData["IsArmor"])
		{
			IsUsingHealingItem = false;
		}
	}

	[EventHandler("gtacnr:died")]
	private void OnDead(int killerId, int cause)
	{
		armorUseTimestamp.Clear();
	}
}
