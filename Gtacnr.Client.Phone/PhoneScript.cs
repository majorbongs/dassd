using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Gtacnr.Client.Anticheat;
using Gtacnr.Client.IMenu;
using Gtacnr.Client.Weapons;
using Gtacnr.Data;
using Gtacnr.Localization;
using Gtacnr.Model;

namespace Gtacnr.Client.Phone;

public class PhoneScript : Script
{
	private static Prop phoneProp;

	private static Prop caseProp;

	private static string lastAnimDict;

	private static string lastAnimName;

	private static WeaponHash previousWeapon;

	private const string DEFAULT_PHONE_MODEL = "prop_amb_phone";

	public static bool IsPhoneOpen => (Entity)(object)phoneProp != (Entity)null;

	public static async Task OpenPhone()
	{
		if ((Entity)(object)phoneProp != (Entity)null)
		{
			await ClosePhone();
		}
		if ((Entity)(object)caseProp != (Entity)null)
		{
			((PoolObject)caseProp).Delete();
		}
		previousWeapon = Game.PlayerPed.Weapons.Current.Hash;
		WeaponBehaviorScript.QuickSwitchToWeapon((WeaponHash)(-1569615261));
		await PlayPhoneAnim("cellphone_text_in");
		await BaseScript.Delay(500);
		string phoneModel = "prop_amb_phone";
		string caseModel = null;
		string phoneItemId = Utils.GetPreference<string>("gtacnr:phoneItem");
		int bone = API.GetPedBoneIndex(((PoolObject)Game.PlayerPed).Handle, 28422);
		if (phoneItemId != null)
		{
			InventoryItem itemInfo = Gtacnr.Data.Items.GetItemDefinition(phoneItemId);
			if (itemInfo != null)
			{
				IEnumerable<InventoryEntry> enumerable = InventoryMenuScript.Cache;
				if (enumerable == null)
				{
					enumerable = await InventoryMenuScript.ReloadInventory();
				}
				if (enumerable.Any((InventoryEntry entry) => entry.ItemId == phoneItemId && entry.Amount > 0f))
				{
					phoneModel = itemInfo.Model;
				}
			}
		}
		string caseItemId = Utils.GetPreference<string>("gtacnr:phoneCase");
		if (caseItemId != null)
		{
			InventoryItem itemInfo = Gtacnr.Data.Items.GetItemDefinition(caseItemId);
			if (itemInfo != null)
			{
				IEnumerable<InventoryEntry> enumerable = InventoryMenuScript.Cache;
				if (enumerable == null)
				{
					enumerable = await InventoryMenuScript.ReloadInventory();
				}
				if (enumerable.Any((InventoryEntry entry) => entry.ItemId == caseItemId && entry.Amount > 0f))
				{
					caseModel = itemInfo.Model;
				}
			}
		}
		List<Entity> entities = new List<Entity>();
		phoneProp = await World.CreateProp(Model.op_Implicit(phoneModel), ((Entity)Game.PlayerPed).Position, false, false);
		API.AttachEntityToEntity(((PoolObject)phoneProp).Handle, ((PoolObject)Game.PlayerPed).Handle, bone, 0f, 0f, 0f, 0f, 0f, 0f, true, true, false, false, 2, true);
		entities.Add((Entity)(object)phoneProp);
		if (caseModel != null)
		{
			caseProp = await World.CreateProp(Model.op_Implicit(caseModel), ((Entity)Game.PlayerPed).Position, false, false);
			API.AttachEntityToEntity(((PoolObject)caseProp).Handle, ((PoolObject)Game.PlayerPed).Handle, bone, 0f, 0f, 0f, 0f, 0f, 0f, true, true, false, false, 2, true);
			entities.Add((Entity)(object)caseProp);
		}
		AntiEntitySpawnScript.RegisterEntities(entities);
	}

	public static async Task ClosePhone()
	{
		string anim = ((lastAnimName == "cellphone_text_in") ? "cellphone_text_out" : "cellphone_call_out");
		List<Prop> propsToRemove = new List<Prop> { phoneProp, caseProp };
		await PlayPhoneAnim(anim);
		await BaseScript.Delay(800);
		foreach (Prop item in propsToRemove)
		{
			if (!((Entity)(object)item == (Entity)null))
			{
				((PoolObject)item).Delete();
			}
		}
		phoneProp = null;
		caseProp = null;
		Game.PlayerPed.Task.ClearAnimation(lastAnimDict, lastAnimName);
		WeaponBehaviorScript.QuickSwitchToWeapon(previousWeapon);
	}

	public static async Task PlayPhoneAnim(string anim)
	{
		string dict = "cellphone@";
		if (Game.PlayerPed.IsInVehicle())
		{
			dict = "anim@cellphone@in_car@ps";
		}
		await Utils.LoadAnimDictionary(dict);
		Game.PlayerPed.Task.PlayAnimation(dict, anim, 3f, -1, (AnimationFlags)50);
		lastAnimDict = dict;
		lastAnimName = anim;
	}

	[EventHandler("gtacnr:inventories:usingItem")]
	private void OnUsingItem(string itemId, float amount)
	{
		InventoryItem itemDefinition = Gtacnr.Data.Items.GetItemDefinition(itemId);
		if (itemDefinition == null)
		{
			return;
		}
		if (itemDefinition.GetExtraDataBool("IsPhone"))
		{
			API.CancelEvent();
			Utils.SetPreference("gtacnr:phoneItem", itemDefinition.Id);
			Utils.DisplayHelpText(LocalizationController.S(Entries.Imenu.IMENU_PHONE_MODEL_CHANGED, itemDefinition.Name), playSound: false);
			string extraDataString = itemDefinition.GetExtraDataString("CaseType");
			if (extraDataString == null)
			{
				return;
			}
			string preference = Utils.GetPreference<string>("gtacnr:phoneCase");
			if (preference != null)
			{
				string extraDataString2 = Gtacnr.Data.Items.GetItemDefinition(preference).GetExtraDataString("CaseType");
				if (extraDataString != extraDataString2)
				{
					Utils.ResetPreference("gtacnr:phoneCase");
				}
			}
		}
		else
		{
			if (!itemDefinition.GetExtraDataBool("IsPhoneCase"))
			{
				return;
			}
			API.CancelEvent();
			if (Utils.GetPreference<string>("gtacnr:phoneCase") == itemId)
			{
				Utils.ResetPreference("gtacnr:phoneCase");
				Utils.DisplayHelpText(LocalizationController.S(Entries.Imenu.IMENU_PHONE_CASE_REMOVED), playSound: false);
				return;
			}
			string extraDataString3 = itemDefinition.GetExtraDataString("CaseType");
			string preference2 = Utils.GetPreference<string>("gtacnr:phoneItem");
			if (preference2 == null)
			{
				Utils.DisplayHelpText(LocalizationController.S(Entries.Imenu.IMENU_PHONE_CASE_NO_PHONE), playSound: false);
				return;
			}
			string extraDataString4 = Gtacnr.Data.Items.GetItemDefinition(preference2).GetExtraDataString("CaseType");
			if (extraDataString3 != extraDataString4)
			{
				Utils.DisplayHelpText(LocalizationController.S(Entries.Imenu.IMENU_PHONE_CASE_TYPE_MISMATCH), playSound: false);
				return;
			}
			Utils.SetPreference("gtacnr:phoneCase", itemDefinition.Id);
			Utils.DisplayHelpText(LocalizationController.S(Entries.Imenu.IMENU_PHONE_CASE_CHANGED, itemDefinition.Name), playSound: false);
		}
	}
}
