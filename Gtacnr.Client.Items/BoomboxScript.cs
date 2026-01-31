using System.Linq;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Gtacnr.Client.IMenu;
using Gtacnr.Client.Jobs.Police.Arrest;
using Gtacnr.Data;
using Gtacnr.Localization;
using Gtacnr.Model;

namespace Gtacnr.Client.Items;

public class BoomboxScript : Script
{
	private Prop boomboxProp;

	private bool tasksAttached;

	public static bool IsUsingBoombox { get; private set; }

	[EventHandler("gtacnr:inventories:usingItem")]
	private void OnUsingItem(string itemId, float amount)
	{
		InventoryItem itemDefinition = Gtacnr.Data.Items.GetItemDefinition(itemId);
		if (itemDefinition == null || !(itemId == "boombox"))
		{
			return;
		}
		if (amount != 1f)
		{
			Utils.SendNotification("You can only use one ~r~" + itemDefinition.Name + " ~s~at a time.");
			Utils.PlayErrorSound();
			API.CancelEvent();
			return;
		}
		if (InventoryMenuScript.Cache != null)
		{
			InventoryEntry inventoryEntry = InventoryMenuScript.Cache.FirstOrDefault((InventoryEntry i) => i.ItemId == itemId);
			if (inventoryEntry == null || inventoryEntry.Amount < 1f)
			{
				Utils.SendNotification("You don't have a ~r~" + itemDefinition.Name + "~s~.");
				Utils.PlayErrorSound();
				API.CancelEvent();
				return;
			}
		}
		if (CuffedScript.IsCuffed)
		{
			Utils.SendNotification(LocalizationController.S(Entries.Player.CANT_USE_WHEN_CUFFED, itemDefinition.Name));
			Utils.PlayErrorSound();
			API.CancelEvent();
		}
		else
		{
			EquipBoombox();
			API.CancelEvent();
		}
	}

	private async void EquipBoombox()
	{
		if (!IsUsingBoombox)
		{
			Game.PlayerPed.Weapons.Select((WeaponHash)(-1569615261));
			IsUsingBoombox = true;
			AttachTasks();
			boomboxProp = await World.CreateProp(Model.op_Implicit(API.GetHashKey(Gtacnr.Data.Items.GetItemDefinition("boombox").Model)), ((Entity)Game.PlayerPed).Position, true, false);
			Prop obj = boomboxProp;
			int pedBoneIndex = API.GetPedBoneIndex(((PoolObject)Game.PlayerPed).Handle, 28422);
			API.AttachEntityToEntity(((PoolObject)obj).Handle, ((PoolObject)Game.PlayerPed).Handle, pedBoneIndex, 0.23f, -0.015f, 0.015f, 186.1f, -269.2f, 111.6f, true, true, false, true, 1, true);
		}
	}

	private void RemoveBoombox()
	{
		if (IsUsingBoombox)
		{
			IsUsingBoombox = false;
			DetachTasks();
			if (!((Entity)(object)boomboxProp == (Entity)null))
			{
				((Entity)boomboxProp).Detach();
				((PoolObject)boomboxProp).Delete();
				string text = $"boombox_{Game.Player.ServerId}";
				((dynamic)((BaseScript)this).Exports["mx-surround"]).Destroy(text);
			}
		}
	}

	private void PlaceBoomboxOnGround()
	{
		RemoveBoombox();
	}

	private void AttachTasks()
	{
		if (IsUsingBoombox && !tasksAttached)
		{
			tasksAttached = true;
			base.Update += BoomboxTask;
		}
	}

	private void DetachTasks()
	{
		if (!IsUsingBoombox && tasksAttached)
		{
			tasksAttached = false;
			base.Update -= BoomboxTask;
		}
	}

	private async Coroutine BoomboxTask()
	{
		await BaseScript.Delay(100);
		if (IsUsingBoombox && ((int)Game.PlayerPed.Weapons.Current.Hash != -1569615261 || Game.PlayerPed.IsInMeleeCombat || CuffedScript.IsCuffed))
		{
			PlaceBoomboxOnGround();
		}
	}
}
