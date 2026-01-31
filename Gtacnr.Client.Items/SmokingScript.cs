using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Gtacnr.Client.Anticheat;
using Gtacnr.Client.API.Scripts;
using Gtacnr.Client.IMenu;
using Gtacnr.Data;
using Gtacnr.Model;
using Gtacnr.Model.Enums;

namespace Gtacnr.Client.Items;

public class SmokingScript : Script
{
	private Prop cigProp;

	protected override void OnStarted()
	{
		KeysScript.AttachListener((Control)37, OnKeyEvent, 1000);
	}

	private bool OnKeyEvent(Control control, KeyEventType eventType, InputType inputType)
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Invalid comparison between Unknown and I4
		if ((Entity)(object)cigProp != (Entity)null && (int)control == 37)
		{
			DropCig();
		}
		return false;
	}

	private void DropCig()
	{
		int oldPropHandle;
		if (!((Entity)(object)cigProp == (Entity)null))
		{
			oldPropHandle = ((PoolObject)cigProp).Handle;
			((Entity)cigProp).Detach();
			Game.PlayerPed.Task.ClearSecondary();
			DeleteOldProp();
		}
		async void DeleteOldProp()
		{
			await BaseScript.Delay(5000);
			API.DeleteObject(ref oldPropHandle);
		}
	}

	[EventHandler("gtacnr:tobacco:smoke")]
	private async void OnSmoke(string itemId, float amount)
	{
		InventoryItem item = Gtacnr.Data.Items.GetItemDefinition(itemId);
		if (item == null || !item.HasExtraData("UseTobaccoScript"))
		{
			return;
		}
		bool isCigar = item.HasExtraData("IsCigar");
		bool isCigarette = item.HasExtraData("IsCigarette");
		bool isJoint = item.HasExtraData("IsJoint");
		bool isVape = item.HasExtraData("IsVape");
		if (!(isCigar || isCigarette || isJoint || isVape))
		{
			return;
		}
		if ((Entity)(object)this.cigProp != (Entity)null)
		{
			DropCig();
		}
		int duration = (isJoint ? 47000 : 32000);
		DateTime t = DateTime.UtcNow;
		string ptfxLibrary = "scr_michael2";
		string ptfxCigSmoke = "cs_cig_smoke";
		string ptfxExhale = "cs_cig_exhale_mouth";
		Sex sex = Utils.GetFreemodePedSex(Game.PlayerPed);
		string dict = ((!(isCigar || isCigarette)) ? (isJoint ? "timetable@gardener@smoking_joint" : "") : ((sex == Sex.Male) ? "amb@world_human_aa_smoke@male@idle_a" : "amb@world_human_smoking@female@idle_a"));
		string name = ((!(isCigar || isCigarette)) ? (isJoint ? "smoke_idle" : "") : ((sex == Sex.Male) ? "idle_c" : "idle_b"));
		await PlayAnim();
		int model = (isCigar ? API.GetHashKey("prop_cigar_03") : (isCigarette ? API.GetHashKey("prop_cs_ciggy_01b") : (isJoint ? API.GetHashKey("p_amb_joint_01") : API.GetHashKey(item.Model))));
		int handBoneIdx = API.GetPedBoneIndex(((PoolObject)Game.PlayerPed).Handle, 28422);
		this.cigProp = await World.CreateProp(Model.op_Implicit(model), ((Entity)Game.PlayerPed).Position, true, false);
		await AntiEntitySpawnScript.RegisterEntity((Entity)(object)this.cigProp);
		Prop cigProp = this.cigProp;
		float[] array = ((!isVape) ? ((!isJoint) ? new float[6] : new float[6] { 0.075f, 0.035f, -0.045f, 21.5f, 115.6f, -245.6f }) : new float[6] { 0f, 0f, 0f, 0f, 90f, 0f });
		API.AttachEntityToEntity(((PoolObject)cigProp).Handle, ((PoolObject)Game.PlayerPed).Handle, handBoneIdx, array[0], array[1], array[2], array[3], array[4], array[5], true, true, false, true, 1, true);
		API.RequestNamedPtfxAsset(ptfxLibrary);
		while (!API.HasNamedPtfxAssetLoaded(ptfxLibrary) && !Gtacnr.Utils.CheckTimePassed(t, 5000.0))
		{
			await BaseScript.Delay(10);
		}
		API.UseParticleFxAssetNextCall(ptfxLibrary);
		int ptfxHandle = 0;
		if (!isVape)
		{
			float num = (isCigar ? (-0.12f) : (isCigarette ? (-0.07f) : (isJoint ? (-0.1f) : 0f)));
			ptfxHandle = API.StartNetworkedParticleFxLoopedOnEntity(ptfxCigSmoke, ((PoolObject)cigProp).Handle, num, 0f, 0f, 0f, 0f, 0f, 1f, false, false, false);
		}
		if ((int)Game.PlayerPed.Weapons.Current.Hash != -1569615261)
		{
			Game.PlayerPed.Weapons.Select((WeaponHash)(-1569615261));
		}
		List<int> exhaleTs = ((!(isCigarette || isCigar)) ? (isJoint ? new List<int> { 4370, 17370, 28460, 40100, 45740 } : new List<int>()) : ((sex == Sex.Male) ? new List<int> { 3700, 7400, 12400, 16100, 21300, 24800, 30000, 31740 } : new List<int> { 3700, 7400, 12400, 16100, 21300, 24800, 30000, 31740 }));
		int curExhaleT = 0;
		while (!Gtacnr.Utils.CheckTimePassed(t, duration))
		{
			await BaseScript.Delay(100);
			if ((int)Weapon.op_Implicit(Game.PlayerPed.Weapons.Current) != -1569615261)
			{
				break;
			}
			if (!API.IsEntityPlayingAnim(((PoolObject)Game.PlayerPed).Handle, dict, name, 3))
			{
				await PlayAnim();
			}
			if (curExhaleT < exhaleTs.Count && Gtacnr.Utils.CheckTimePassed(t, exhaleTs[curExhaleT]))
			{
				curExhaleT++;
				API.UseParticleFxAssetNextCall(ptfxLibrary);
				API.StartNetworkedParticleFxNonLoopedOnPedBone(ptfxExhale, ((PoolObject)Game.PlayerPed).Handle, 0f, 0f, 0f, 0f, 0f, 270f, 46240, 1.5f, false, false, false);
			}
		}
		Game.PlayerPed.Task.ClearSecondary();
		if ((Entity)(object)cigProp != (Entity)null)
		{
			((Entity)cigProp).Detach();
			((PoolObject)cigProp).Delete();
			if (!isVape)
			{
				model = (isCigarette ? API.GetHashKey("ng_proc_cigbuts01a") : model);
				this.cigProp = await World.CreateProp(Model.op_Implicit(model), ((Entity)Game.PlayerPed).Position, true, false);
				await AntiEntitySpawnScript.RegisterEntity((Entity)(object)this.cigProp);
				cigProp = this.cigProp;
				API.AttachEntityToEntity(((PoolObject)cigProp).Handle, ((PoolObject)Game.PlayerPed).Handle, handBoneIdx, 0f, 0f, 0f, 0f, 0f, 0f, true, true, false, true, 1, true);
				DropCig();
			}
		}
		if (ptfxHandle != 0)
		{
			API.StopParticleFxLooped(ptfxHandle, false);
			API.RemoveNamedPtfxAsset(ptfxLibrary);
		}
		async Task PlayAnim()
		{
			await Game.PlayerPed.Task.PlayAnimation(dict, name, 4f, -4f, -1, (AnimationFlags)51, 0f);
		}
	}

	[EventHandler("gtacnr:inventories:usingItem")]
	private async void OnUsingItem(string itemId, float amount)
	{
		InventoryItem item = Gtacnr.Data.Items.GetItemDefinition(itemId);
		if (!item.HasExtraData("UseTobaccoScript"))
		{
			return;
		}
		if (((Entity)Game.PlayerPed).IsOnFire)
		{
			Utils.SendNotification("You can't smoke when you're ~r~on fire~s~.");
			API.CancelEvent();
		}
		else if (Game.PlayerPed.IsBeingStunned)
		{
			Utils.SendNotification("You can't smoke when you're ~r~being stunned~s~.");
			API.CancelEvent();
		}
		else if (DrugScript.IsOverdosing)
		{
			Utils.SendNotification("You can't smoke when you're suffering from an ~r~overdose~s~.");
			API.CancelEvent();
		}
		else
		{
			if (!item.HasExtraData("IsCigar") && !item.HasExtraData("IsCigarette") && !item.HasExtraData("IsJoint"))
			{
				return;
			}
			IEnumerable<InventoryEntry> inventory = InventoryMenuScript.Cache;
			if (inventory == null)
			{
				await InventoryMenuScript.ReloadInventory();
			}
			InventoryEntry inventoryEntry = inventory.FirstOrDefault((InventoryEntry entry) => Gtacnr.Data.Items.GetItemDefinition(entry.ItemId)?.HasExtraData("IsLighter") ?? false);
			if (inventoryEntry == null || inventoryEntry.Amount < 1f)
			{
				API.CancelEvent();
				Utils.SendNotification("You need a ~r~lighter ~s~to light up cigarettes.");
			}
			else if (item.HasExtraData("IsJoint"))
			{
				InventoryEntry inventoryEntry2 = inventory.FirstOrDefault((InventoryEntry entry) => Gtacnr.Data.Items.GetItemDefinition(entry.ItemId)?.HasExtraData("IsRollingPapers") ?? false);
				if (inventoryEntry2 == null || inventoryEntry2.Amount < 1f)
				{
					API.CancelEvent();
					Utils.SendNotification("You need ~r~rolling papers ~s~to roll joints.");
				}
			}
		}
	}
}
