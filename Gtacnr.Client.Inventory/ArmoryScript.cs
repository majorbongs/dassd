using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Gtacnr.Client.Admin;
using Gtacnr.Client.API;
using Gtacnr.Client.Characters.Editor;
using Gtacnr.Client.Characters.Lifecycle;
using Gtacnr.Client.Events.Holidays.Christmas;
using Gtacnr.Client.Zones;
using Gtacnr.Data;
using Gtacnr.Localization;
using Gtacnr.Model;

namespace Gtacnr.Client.Inventory;

public class ArmoryScript : Script
{
	private static ArmoryScript instance;

	private bool isReloadingLoadout;

	private Loadout currentLoadout = new Loadout();

	private bool initialized;

	public static Loadout CurrentLoadout => instance.currentLoadout;

	public static bool Initialized => instance.initialized;

	public static event EventHandler LoadoutChanged;

	public static event EventHandler<ArmoryWeaponEventArgs> WeaponEquipped;

	public static event EventHandler<ArmoryWeaponEventArgs> WeaponUnequipped;

	public ArmoryScript()
	{
		instance = this;
		Gtacnr.Client.API.Jobs.JobChangedEvent += OnJobChangedEvent;
	}

	private void SetAmmoByType(int ammoType, int amount)
	{
		int pedAmmoByType = API.GetPedAmmoByType(((PoolObject)Game.PlayerPed).Handle, ammoType);
		AddAmmoByType(ammoType, amount - pedAmmoByType);
	}

	private void AddAmmoByType(int ammoType, int amount)
	{
		API.AddAmmoToPedByType(((PoolObject)Game.PlayerPed).Handle, ammoType, amount);
	}

	private async void OnJobChangedEvent(object sender, JobArgs e)
	{
		if (!(e.PreviousJobId == e.CurrentJobId) && Gtacnr.Data.Jobs.GetJobData(e.CurrentJobId) != null)
		{
			await BaseScript.Delay(1000);
			await instance.ReloadLoadoutInternal();
		}
	}

	public static async Task ReloadLoadout()
	{
		await instance.ReloadLoadoutInternal();
	}

	private async Task ReloadLoadoutInternal()
	{
		if (isReloadingLoadout)
		{
			Print("Attempted to reload loadout while already in progress.");
			return;
		}
		isReloadingLoadout = true;
		try
		{
			Ped playerPed = Game.PlayerPed;
			playerPed.Weapons.RemoveAll();
			foreach (AmmoDefinition allAmmoDefinition in Gtacnr.Data.Items.GetAllAmmoDefinitions())
			{
				int hashKey = API.GetHashKey(allAmmoDefinition.Id);
				SetAmmoByType(hashKey, 0);
			}
			string text = await TriggerServerEventAsync<string>("gtacnr:armory:getContent", new object[0]);
			if (string.IsNullOrWhiteSpace(text))
			{
				Utils.DisplayHelpText(LocalizationController.S(Entries.Main.ERROR_LOADING_LOADOUT));
				return;
			}
			currentLoadout = text?.Unjson<Loadout>();
			if (currentLoadout != null)
			{
				foreach (LoadoutWeapon weaponDatum in currentLoadout.WeaponData)
				{
					string weaponEquippedPreferenceKey = GetWeaponEquippedPreferenceKey((WeaponHash)weaponDatum.Hash);
					weaponDatum.IsEquipped = Utils.GetPreference(weaponEquippedPreferenceKey, defaultValue: true);
					if (!weaponDatum.IsEquipped)
					{
						continue;
					}
					API.GiveWeaponToPed(((PoolObject)playerPed).Handle, weaponDatum.Hash, 0, false, false);
					if (weaponDatum.Attachments == null)
					{
						continue;
					}
					foreach (LoadoutAttachment attachment in weaponDatum.Attachments)
					{
						weaponEquippedPreferenceKey = GetAttachmentEquippedPreferenceKey((WeaponHash)weaponDatum.Hash, (WeaponComponentHash)attachment.Hash);
						attachment.IsEquipped = Utils.GetPreference(weaponEquippedPreferenceKey, defaultValue: true);
						if (attachment.IsEquipped)
						{
							API.GiveWeaponComponentToPed(((PoolObject)playerPed).Handle, weaponDatum.Hash, attachment.Hash);
						}
					}
				}
				foreach (LoadoutAmmo ammoDatum in currentLoadout.AmmoData)
				{
					SetAmmoByType((int)ammoDatum.Hash, ammoDatum.Amount);
				}
			}
			else
			{
				currentLoadout = new Loadout();
			}
			SnowballScript.ResetSnowballs();
		}
		catch (Exception exception)
		{
			Print(exception);
		}
		finally
		{
			isReloadingLoadout = false;
			initialized = true;
			ArmoryScript.LoadoutChanged?.Invoke(this, new EventArgs());
		}
	}

	[EventHandler("gtacnr:armory:getAllAmmo")]
	private void OnGetAllAmmo(int token)
	{
		List<LoadoutAmmo> list = new List<LoadoutAmmo>();
		try
		{
			foreach (AmmoDefinition allAmmoDefinition in Gtacnr.Data.Items.GetAllAmmoDefinitions())
			{
				int hashKey = API.GetHashKey(allAmmoDefinition.Id);
				int pedAmmoByType = API.GetPedAmmoByType(API.PlayerPedId(), hashKey);
				LoadoutAmmo item = new LoadoutAmmo
				{
					ItemId = allAmmoDefinition.Id,
					Amount = pedAmmoByType
				};
				if (pedAmmoByType > 0)
				{
					list.Add(item);
				}
			}
		}
		catch (Exception exception)
		{
			Print(exception);
		}
		Respond(list);
		void Respond(List<LoadoutAmmo> response)
		{
			BaseScript.TriggerServerEvent("gtacnr:armory:getAllAmmo:response", new object[2]
			{
				token,
				response.Json()
			});
		}
	}

	[EventHandler("gtacnr:armory:getChangedAmmo")]
	private void OnGetChangedAmmo(int token)
	{
		try
		{
			List<LoadoutAmmo> list = new List<LoadoutAmmo>();
			if (Utils.IsFreemodePed(Game.PlayerPed))
			{
				try
				{
					foreach (AmmoDefinition ammoDefinition in Gtacnr.Data.Items.GetAllAmmoDefinitions())
					{
						int hashKey = API.GetHashKey(ammoDefinition.Id);
						int pedAmmoByType = API.GetPedAmmoByType(API.PlayerPedId(), hashKey);
						LoadoutAmmo loadoutAmmo = new LoadoutAmmo
						{
							ItemId = ammoDefinition.Id,
							Amount = pedAmmoByType
						};
						LoadoutAmmo loadoutAmmo2 = currentLoadout.AmmoData.FirstOrDefault((LoadoutAmmo a) => a.ItemId == ammoDefinition.Id);
						if (loadoutAmmo2 == null)
						{
							currentLoadout.AmmoData.Add(new LoadoutAmmo
							{
								ItemId = ammoDefinition.Id,
								Amount = pedAmmoByType
							});
						}
						else if (pedAmmoByType != loadoutAmmo2.Amount && !((Entity)(object)Game.PlayerPed == (Entity)null) && currentLoadout != null && SpawnScript.HasSpawned && !isReloadingLoadout && Utils.IsFreemodePed(Game.PlayerPed) && !CharacterCreationScript.IsInCreator)
						{
							loadoutAmmo2.Amount = loadoutAmmo.Amount;
							list.Add(loadoutAmmo);
						}
					}
				}
				catch (Exception exception)
				{
					Print(exception);
				}
			}
			Respond(list);
		}
		catch (Exception exception2)
		{
			Print(exception2);
		}
		void Respond(List<LoadoutAmmo> response)
		{
			BaseScript.TriggerLatentServerEvent("gtacnr:armory:getChangedAmmo:response", 20000, new object[2]
			{
				token,
				response.Json()
			});
		}
	}

	[EventHandler("gtacnr:armory:weaponReceived")]
	private void OnWeaponReceived(string weaponId, string jWeaponData)
	{
		try
		{
			if ((int)StaffLevelScript.StaffLevel > 0)
			{
				Debug.WriteLine("gtacnr:armory:weaponReceived " + weaponId + " " + jWeaponData);
			}
			uint hashKey = (uint)API.GetHashKey(weaponId);
			bool flag = SafezoneScript.Current == null || Gtacnr.Client.API.Jobs.CachedJobEnum.IsPublicService();
			API.GiveWeaponToPed(((PoolObject)Game.PlayerPed).Handle, hashKey, 0, false, flag);
			if (currentLoadout.WeaponData.FirstOrDefault((LoadoutWeapon w) => w.ItemId == weaponId) == null)
			{
				currentLoadout.WeaponData.Add(new LoadoutWeapon
				{
					ItemId = weaponId
				});
			}
			if (!string.IsNullOrEmpty(jWeaponData))
			{
				InventoryEntryData inventoryEntryData = jWeaponData.Unjson<InventoryEntryData>();
				InventoryEntryWeaponData weapon = inventoryEntryData.Weapon;
				if (weapon != null && weapon.Components.Count > 0)
				{
					foreach (string item in inventoryEntryData.Weapon?.Components)
					{
						OnAttachmentReceived(weaponId, item);
					}
				}
			}
			ArmoryWeaponEventArgs e = new ArmoryWeaponEventArgs((WeaponHash)hashKey);
			ArmoryScript.LoadoutChanged?.Invoke(this, e);
			ArmoryScript.WeaponEquipped?.Invoke(this, e);
		}
		catch (Exception exception)
		{
			Print(exception);
		}
	}

	[EventHandler("gtacnr:armory:weaponRemoved")]
	private void OnWeaponRemoved(string weaponId)
	{
		try
		{
			if ((int)StaffLevelScript.StaffLevel > 0)
			{
				Debug.WriteLine("gtacnr:armory:weaponRemoved " + weaponId);
			}
			uint hashKey = (uint)API.GetHashKey(weaponId);
			API.RemoveWeaponFromPed(((PoolObject)Game.PlayerPed).Handle, hashKey);
			LoadoutWeapon loadoutWeapon = currentLoadout.WeaponData.FirstOrDefault((LoadoutWeapon w) => w.ItemId == weaponId);
			if (loadoutWeapon != null)
			{
				currentLoadout.WeaponData.Remove(loadoutWeapon);
			}
			ArmoryWeaponEventArgs e = new ArmoryWeaponEventArgs((WeaponHash)hashKey);
			ArmoryScript.LoadoutChanged?.Invoke(this, e);
			ArmoryScript.WeaponUnequipped?.Invoke(this, e);
		}
		catch (Exception exception)
		{
			Print(exception);
		}
	}

	[EventHandler("gtacnr:armory:attachmentReceived")]
	private void OnAttachmentReceived(string weaponId, string componentId)
	{
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			WeaponHash val = (WeaponHash)Gtacnr.Utils.GenerateHash(weaponId);
			if (!Game.PlayerPed.Weapons.HasWeapon(val))
			{
				Debug.WriteLine("^2Warning: ^0Ignoring received attachment `" + componentId + "` without owning the required weapon `" + weaponId + "`.");
				return;
			}
			API.GiveWeaponComponentToPed(((PoolObject)Game.PlayerPed).Handle, (uint)Gtacnr.Utils.GenerateHash(weaponId), (uint)Gtacnr.Utils.GenerateHash(componentId));
			LoadoutWeapon loadoutWeapon = currentLoadout.WeaponData.FirstOrDefault((LoadoutWeapon w) => w.ItemId == weaponId);
			if (loadoutWeapon == null)
			{
				currentLoadout.WeaponData.Add(new LoadoutWeapon
				{
					ItemId = weaponId
				});
			}
			if (loadoutWeapon.Attachments == null)
			{
				loadoutWeapon.Attachments = new List<LoadoutAttachment>();
			}
			if (loadoutWeapon.Attachments.FirstOrDefault((LoadoutAttachment a) => a.ItemId == componentId) == null)
			{
				loadoutWeapon.Attachments.Add(new LoadoutAttachment
				{
					ItemId = componentId
				});
			}
		}
		catch (Exception exception)
		{
			Print(exception);
		}
	}

	[EventHandler("gtacnr:armory:ammoReceived")]
	private void OnAmmoReceived(string ammoId, int amountToAdd)
	{
		try
		{
			int hashKey = API.GetHashKey(ammoId);
			int num = API.GetPedAmmoByType(((PoolObject)Game.PlayerPed).Handle, hashKey) + amountToAdd;
			if (num < 0)
			{
				num = 0;
			}
			LoadoutAmmo loadoutAmmo = currentLoadout.AmmoData.FirstOrDefault((LoadoutAmmo a) => a.ItemId == ammoId);
			if (loadoutAmmo != null)
			{
				loadoutAmmo.Amount = num;
			}
			else
			{
				currentLoadout.AmmoData.Add(new LoadoutAmmo
				{
					ItemId = ammoId,
					Amount = num
				});
			}
			AddAmmoByType(hashKey, amountToAdd);
		}
		catch (Exception exception)
		{
			Print(exception);
		}
	}

	[Update]
	private async Coroutine CheckWeaponsTask()
	{
		await Script.Wait(1000);
		if ((Entity)(object)Game.PlayerPed == (Entity)null || currentLoadout == null || !SpawnScript.HasSpawned || isReloadingLoadout || !Utils.IsFreemodePed(Game.PlayerPed) || CharacterCreationScript.IsInCreator)
		{
			return;
		}
		int num = 0;
		int num2 = 0;
		bool flag = false;
		List<WeaponHash> list = new List<WeaponHash>
		{
			(WeaponHash)(-72657034),
			(WeaponHash)(-1813897027),
			(WeaponHash)615608432,
			(WeaponHash)Gtacnr.Utils.GenerateHash("weapon_flashbang"),
			(WeaponHash)(-37975472),
			(WeaponHash)(-1600701090)
		};
		List<WeaponHash> list2 = new List<WeaponHash>
		{
			(WeaponHash)(-1569615261),
			(WeaponHash)126349499
		};
		foreach (WeaponDefinition weaponDef in Gtacnr.Data.Items.GetAllWeaponDefinitions())
		{
			if (!API.IsWeaponValid((uint)weaponDef.Hash))
			{
				continue;
			}
			LoadoutWeapon loadoutWeapon = currentLoadout.WeaponData.FirstOrDefault((LoadoutWeapon w) => w.ItemId == weaponDef.Id);
			bool flag2 = loadoutWeapon != null;
			bool flag3 = Game.PlayerPed.Weapons.HasWeapon((WeaponHash)API.GetHashKey(weaponDef.Id));
			uint hashKey = (uint)API.GetHashKey(weaponDef.Id);
			if (flag2 && !flag3 && loadoutWeapon.IsEquipped && !list.Contains((WeaponHash)hashKey))
			{
				num++;
				API.GiveWeaponToPed(((PoolObject)Game.PlayerPed).Handle, loadoutWeapon.Hash, 0, false, false);
				foreach (LoadoutAttachment attachment in loadoutWeapon.Attachments)
				{
					API.GiveWeaponComponentToPed(((PoolObject)Game.PlayerPed).Handle, loadoutWeapon.Hash, attachment.Hash);
				}
				if (hashKey == 883325847)
				{
					flag = true;
				}
			}
			else if (!flag2 && flag3 && !list2.Contains((WeaponHash)hashKey))
			{
				num2++;
				API.RemoveWeaponFromPed(((PoolObject)Game.PlayerPed).Handle, hashKey);
				if (hashKey == 4222310262u)
				{
					flag = true;
				}
			}
		}
		if (num != 0 && !flag)
		{
			Utils.SendNotification(LocalizationController.S(Entries.Chatnotifications.SHIELD_WEAPON_RESTORE, num));
			foreach (LoadoutAmmo ammoDatum in currentLoadout.AmmoData)
			{
				SetAmmoByType((int)ammoDatum.Hash, ammoDatum.Amount);
			}
		}
		if (num2 != 0 && !flag)
		{
			Utils.SendNotification(LocalizationController.S(Entries.Chatnotifications.SHIELD_WEAPON_REMOVE, num2));
		}
	}

	public static bool EquipWeapon(WeaponHash weaponHash, bool equip = true)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0179: Unknown result type (might be due to invalid IL or missing references)
		//IL_0194: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_015d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0124: Unknown result type (might be due to invalid IL or missing references)
		//IL_0135: Expected I4, but got Unknown
		LoadoutWeapon loadoutWeapon = CurrentLoadout.WeaponData.FirstOrDefault((LoadoutWeapon lw) => lw.Hash == (uint)(int)weaponHash);
		if (loadoutWeapon == null)
		{
			instance.Print($"You don't own {weaponHash:X}");
			return false;
		}
		string weaponEquippedPreferenceKey = GetWeaponEquippedPreferenceKey(weaponHash);
		if (Utils.GetPreference(weaponEquippedPreferenceKey, defaultValue: true) == equip)
		{
			instance.Print(string.Format("{0:X} is already {1}", weaponHash, equip ? "equipped" : "unequipped"));
			return false;
		}
		Utils.SetPreference(weaponEquippedPreferenceKey, equip);
		loadoutWeapon.IsEquipped = equip;
		if (equip)
		{
			bool flag = SafezoneScript.Current == null || Gtacnr.Client.API.Jobs.CachedJobEnum.IsPublicService();
			Game.PlayerPed.Weapons.Give(weaponHash, 0, flag, true);
			if (loadoutWeapon.Attachments != null)
			{
				foreach (LoadoutAttachment item in loadoutWeapon.Attachments.Where((LoadoutAttachment a) => a.IsEquipped))
				{
					API.GiveWeaponComponentToPed(((PoolObject)Game.PlayerPed).Handle, (uint)(int)weaponHash, item.Hash);
				}
			}
			ArmoryScript.WeaponEquipped?.Invoke(instance, new ArmoryWeaponEventArgs(weaponHash));
		}
		else
		{
			Game.PlayerPed.Weapons.Remove(weaponHash);
			ArmoryScript.WeaponUnequipped?.Invoke(instance, new ArmoryWeaponEventArgs(weaponHash));
		}
		instance.Print(string.Format("{0:X} has been {1}", weaponHash, equip ? "equipped" : "unequipped"));
		return true;
	}

	public static bool UnequipWeapon(WeaponHash weaponHash)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		return EquipWeapon(weaponHash, equip: false);
	}

	public static bool IsWeaponEquipped(WeaponHash weaponHash)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		return Utils.GetPreference(GetWeaponEquippedPreferenceKey(weaponHash), defaultValue: true);
	}

	public static bool EquipAttachment(WeaponHash weaponHash, WeaponComponentHash attachmentHash, bool equip = true)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_010b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_015d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0168: Unknown result type (might be due to invalid IL or missing references)
		//IL_0142: Unknown result type (might be due to invalid IL or missing references)
		//IL_0148: Unknown result type (might be due to invalid IL or missing references)
		//IL_0152: Expected I4, but got Unknown
		//IL_0152: Expected I4, but got Unknown
		//IL_0125: Unknown result type (might be due to invalid IL or missing references)
		//IL_012b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0135: Expected I4, but got Unknown
		//IL_0135: Expected I4, but got Unknown
		LoadoutWeapon loadoutWeapon = CurrentLoadout.WeaponData.FirstOrDefault((LoadoutWeapon lw) => lw.Hash == (uint)(int)weaponHash);
		if (loadoutWeapon == null)
		{
			instance.Print($"You don't own {weaponHash:X}");
			return false;
		}
		LoadoutAttachment loadoutAttachment = loadoutWeapon.Attachments.FirstOrDefault((LoadoutAttachment la) => la.Hash == (uint)(int)attachmentHash);
		if (loadoutAttachment == null)
		{
			instance.Print($"You don't own {attachmentHash:X} on {weaponHash:X}");
			return false;
		}
		string attachmentEquippedPreferenceKey = GetAttachmentEquippedPreferenceKey(weaponHash, attachmentHash);
		if (Utils.GetPreference(attachmentEquippedPreferenceKey, defaultValue: true) == equip)
		{
			instance.Print(string.Format("{0:X} on {1:X} is already {2}", attachmentHash, weaponHash, equip ? "equipped" : "unequipped"));
			return false;
		}
		Utils.SetPreference(attachmentEquippedPreferenceKey, equip);
		loadoutAttachment.IsEquipped = equip;
		if (Game.PlayerPed.Weapons.HasWeapon(weaponHash))
		{
			if (equip)
			{
				API.GiveWeaponComponentToPed(((PoolObject)Game.PlayerPed).Handle, (uint)(int)weaponHash, (uint)(int)attachmentHash);
			}
			else
			{
				API.RemoveWeaponComponentFromPed(((PoolObject)Game.PlayerPed).Handle, (uint)(int)weaponHash, (uint)(int)attachmentHash);
			}
		}
		instance.Print(string.Format("{0:X} on {1:X} has been {2}", attachmentHash, weaponHash, equip ? "equipped" : "unequipped"));
		return true;
	}

	public static bool UnequipAttachment(WeaponHash weaponHash, WeaponComponentHash attachmentHash)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		return EquipAttachment(weaponHash, attachmentHash, equip: false);
	}

	public static bool IsAttachmentEquipped(WeaponHash weaponHash, WeaponComponentHash attachmentHash)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		return Utils.GetPreference(GetAttachmentEquippedPreferenceKey(weaponHash, attachmentHash), defaultValue: true);
	}

	public static string GetWeaponEquippedPreferenceKey(WeaponHash weaponHash)
	{
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Expected I4, but got Unknown
		Job jobData = Gtacnr.Data.Jobs.GetJobData(Gtacnr.Client.API.Jobs.CachedJob);
		string arg = ((jobData == null) ? "_none" : (jobData.SeparateLoadout ? ("_" + jobData.Id) : "_none"));
		return $"{(int)weaponHash:X}{arg}_equipped";
	}

	public static string GetAttachmentEquippedPreferenceKey(WeaponHash weaponHash, WeaponComponentHash attachmentHash)
	{
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Expected I4, but got Unknown
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Expected I4, but got Unknown
		Job jobData = Gtacnr.Data.Jobs.GetJobData(Gtacnr.Client.API.Jobs.CachedJob);
		string arg = ((jobData == null) ? "_none" : (jobData.SeparateLoadout ? ("_" + jobData.Id) : "_none"));
		return $"{(int)weaponHash:X}_{(int)attachmentHash:X}{arg}_equipped";
	}

	public static bool HasWeapon(WeaponHash weaponHash)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		return CurrentLoadout.WeaponData.Any((LoadoutWeapon lw) => lw.Hash == (uint)(int)weaponHash);
	}

	public static bool HasAttachment(WeaponHash weaponHash, WeaponComponentHash attachmentHash)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		if (!HasWeapon(weaponHash))
		{
			return false;
		}
		LoadoutWeapon loadoutWeapon = CurrentLoadout.WeaponData.First((LoadoutWeapon lw) => lw.Hash == (uint)(int)weaponHash);
		if (loadoutWeapon.Attachments == null)
		{
			return false;
		}
		return loadoutWeapon.Attachments.Any((LoadoutAttachment la) => la.Hash == (uint)(int)attachmentHash);
	}
}
