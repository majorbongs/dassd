using System;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Gtacnr.Client.Anticheat;
using Gtacnr.Client.API.Scripts;
using Gtacnr.Data;
using Gtacnr.Localization;
using Gtacnr.Model;

namespace Gtacnr.Client.Items;

public class FoodScript : Script
{
	private DateTime lastConsumedT = DateTime.MinValue;

	private Prop foodProp;

	protected override void OnStarted()
	{
		KeysScript.AttachListener((Control)37, OnKeyEvent, 1000);
	}

	private bool OnKeyEvent(Control control, KeyEventType eventType, InputType inputType)
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Invalid comparison between Unknown and I4
		if ((Entity)(object)foodProp != (Entity)null && (int)control == 37)
		{
			DropFood();
		}
		return false;
	}

	private void DropFood()
	{
		int oldPropHandle;
		if (!((Entity)(object)foodProp == (Entity)null))
		{
			oldPropHandle = ((PoolObject)foodProp).Handle;
			((Entity)foodProp).Detach();
			Game.PlayerPed.Task.ClearSecondary();
			DeleteOldFood();
		}
		async void DeleteOldFood()
		{
			await BaseScript.Delay(5000);
			API.DeleteObject(ref oldPropHandle);
		}
	}

	[EventHandler("gtacnr:inventories:usingItem")]
	private void OnUsingItem(string itemId, float amount)
	{
		InventoryItem itemDefinition = Gtacnr.Data.Items.GetItemDefinition(itemId);
		if (itemDefinition != null && itemDefinition.HasExtraData("NutritionalValue") && !Gtacnr.Utils.CheckTimePassed(lastConsumedT, Constants.Food.FoodConsumptionCooldown))
		{
			Utils.SendNotification(LocalizationController.S(Entries.Specialitems.SPECIALITEM_REPAIRKIT_COOLDOWN, Gtacnr.Utils.GetCooldownTimeLeft(lastConsumedT, Constants.Food.FoodConsumptionCooldown).Seconds, itemDefinition.Name));
			Utils.PlayErrorSound();
			API.CancelEvent();
		}
	}

	[EventHandler("gtacnr:food:eat")]
	private async void OnEat(string itemId, float amount)
	{
		InventoryItem item = Gtacnr.Data.Items.GetItemDefinition(itemId);
		if (item == null)
		{
			return;
		}
		lastConsumedT = DateTime.UtcNow;
		Prop foodProp;
		Prop secondProp;
		int time;
		int time2;
		if (item.HasExtraData("AnimDict") && item.HasExtraData("AnimName"))
		{
			string extraDataString = item.GetExtraDataString("AnimDict");
			string extraDataString2 = item.GetExtraDataString("AnimName");
			AnimationFlags val = (AnimationFlags)item.GetExtraDataInt("AnimFlags");
			int duration = item.GetExtraDataInt("AnimTime");
			if ((Entity)(object)this.foodProp != (Entity)null)
			{
				DropFood();
			}
			await Game.PlayerPed.Task.PlayAnimation(extraDataString, extraDataString2, 4f, -4f, duration, val, 0f);
			int delay = 0;
			if (item.HasExtraData("SpawnFirstPropAfter"))
			{
				delay = item.GetExtraDataInt("SpawnFirstPropAfter");
				await BaseScript.Delay(delay);
			}
			foodProp = null;
			if (!string.IsNullOrEmpty(item.Model) && item.HasExtraData("PropBone") && item.HasExtraData("PropPlacement"))
			{
				int hashKey = API.GetHashKey(item.Model);
				Vector3 position = ((Entity)Game.PlayerPed).Position;
				this.foodProp = await World.CreateProp(Model.op_Implicit(hashKey), position, true, false);
				AntiEntitySpawnScript.RegisterEntity((Entity)(object)this.foodProp);
				foodProp = this.foodProp;
				int extraDataInt = item.GetExtraDataInt("PropBone");
				float[] extraDataFloatArray = item.GetExtraDataFloatArray("PropPlacement");
				API.AttachEntityToEntity(((PoolObject)foodProp).Handle, ((PoolObject)Game.PlayerPed).Handle, API.GetPedBoneIndex(((PoolObject)Game.PlayerPed).Handle, extraDataInt), extraDataFloatArray[0], extraDataFloatArray[1], extraDataFloatArray[2], extraDataFloatArray[3], extraDataFloatArray[4], extraDataFloatArray[5], true, true, false, true, 1, true);
			}
			secondProp = null;
			if (item.HasExtraData("SecondModel") && item.HasExtraData("SecondPropBone") && item.HasExtraData("SecondPropPlacement"))
			{
				string extraDataString3 = item.GetExtraDataString("SecondModel");
				int bone = item.GetExtraDataInt("SecondPropBone");
				float[] placement = item.GetExtraDataFloatArray("SecondPropPlacement");
				int hashKey2 = API.GetHashKey(extraDataString3);
				Vector3 position2 = ((Entity)Game.PlayerPed).Position;
				secondProp = await World.CreateProp(Model.op_Implicit(hashKey2), position2, true, false);
				AntiEntitySpawnScript.RegisterEntity((Entity)(object)secondProp);
				API.AttachEntityToEntity(((PoolObject)secondProp).Handle, ((PoolObject)Game.PlayerPed).Handle, API.GetPedBoneIndex(((PoolObject)Game.PlayerPed).Handle, bone), placement[0], placement[1], placement[2], placement[3], placement[4], placement[5], true, true, false, true, 1, true);
			}
			if (item.HasExtraData("DeleteFirstPropAfter"))
			{
				time = item.GetExtraDataInt("DeleteFirstPropAfter");
				DeleteFirstProp();
			}
			if (item.HasExtraData("DeleteSecondPropAfter"))
			{
				time2 = item.GetExtraDataInt("DeleteSecondPropAfter");
				DeleteSecondProp();
			}
			if ((int)Game.PlayerPed.Weapons.Current.Hash != -1569615261)
			{
				Game.PlayerPed.Weapons.Select((WeaponHash)(-1569615261));
			}
			if (duration <= 0)
			{
				duration = 6000;
			}
			await BaseScript.Delay(duration - delay);
			Game.PlayerPed.Task.ClearSecondary();
			if ((Entity)(object)foodProp != (Entity)null)
			{
				((Entity)foodProp).Detach();
				((PoolObject)foodProp).Delete();
				foodProp = null;
			}
			if ((Entity)(object)secondProp != (Entity)null)
			{
				((Entity)secondProp).Detach();
				((PoolObject)secondProp).Delete();
			}
		}
		async void DeleteFirstProp()
		{
			await BaseScript.Delay(time);
			((PoolObject)foodProp).Delete();
		}
		async void DeleteSecondProp()
		{
			await BaseScript.Delay(time2);
			((PoolObject)secondProp).Delete();
		}
	}
}
