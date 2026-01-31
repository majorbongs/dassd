using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Gtacnr.Client.Anticheat;
using Gtacnr.Client.API;
using Gtacnr.Client.API.Scripts;
using Gtacnr.Client.API.UI;
using Gtacnr.Client.API.UI.Menus;
using Gtacnr.Client.HUD;
using Gtacnr.Client.IMenu;
using Gtacnr.Client.Jobs.Police.Arrest;
using Gtacnr.Data;
using Gtacnr.Localization;
using Gtacnr.Model;
using Gtacnr.Model.Enums;
using Gtacnr.ResponseCodes;
using MenuAPI;

namespace Gtacnr.Client.Businesses.Banks;

public class ATMScript : Script
{
	private readonly ReadOnlyCollection<long> atmAmounts = new ReadOnlyCollection<long>(new List<long> { 50000L, 100000L, 250000L, 500000L, 1000000L, 2000000L });

	private Prop? closestAtmProp;

	private Prop? prevClosestAtmProp;

	private bool canUse;

	private bool canWithdraw;

	private bool canDeposit;

	private bool isBusy;

	private DateTime lastHackTimestamp = DateTime.MinValue;

	public Menu atmMenu;

	private Dictionary<string, MenuItem> menuItems = new Dictionary<string, MenuItem>();

	private List<long> depositAmounts = new List<long>();

	private List<long> withdrawAmounts = new List<long>();

	private bool controlsEnabled;

	public static bool IsHacking { get; private set; }

	protected override void OnStarted()
	{
		atmMenu = new Menu(LocalizationController.S(Entries.Banking.MENU_ATM_TITLE), LocalizationController.S(Entries.Banking.MENU_ATM_SUBTITLE))
		{
			MaxDistance = 7.5f
		};
		MenuController.AddMenu(atmMenu);
		RefreshMenu();
		atmMenu.OnMenuClose += OnMenuClose;
		atmMenu.OnItemSelect += OnMenuItemSelect;
		atmMenu.OnListItemSelect += OnMenuListItemSelect;
		atmMenu.CounterPreText = "~r~" + LocalizationController.S(Entries.Banking.MENU_ATM_FEE, 3500.ToCurrencyString());
		API.DecorRegister("gtacnr:atmOutOfService", 3);
	}

	private async void OpenMenu()
	{
		if (!atmMenu.Visible && !((Entity)(object)closestAtmProp == (Entity)null))
		{
			if (CuffedScript.IsBeingCuffedOrUncuffed || CuffedScript.IsCuffed || CuffedScript.IsInCustody)
			{
				Utils.DisplayHelpText(LocalizationController.S(Entries.Banking.ATM_CUFFED));
				return;
			}
			if (API.DecorGetInt(((PoolObject)closestAtmProp).Handle, "gtacnr:atmOutOfService") == 1)
			{
				Utils.DisplayHelpText(LocalizationController.S(Entries.Banking.ATM_OUT_OF_SERVICE));
				return;
			}
			DisableControls();
			Menus.CloseAll();
			atmMenu.ClearMenuItems();
			atmMenu.AddLoadingMenuItem();
			atmMenu.Visible = true;
			MoneyDisplayScript.ForceMoneyDisplay = true;
			RefreshMenu();
			Game.PlayerPed.Task.ClearAllImmediately();
			API.TaskTurnPedToFaceEntity(API.PlayerPedId(), ((PoolObject)closestAtmProp).Handle, 1000);
			await BaseScript.Delay(1000);
			API.TaskStartScenarioInPlace(API.PlayerPedId(), "PROP_HUMAN_ATM", 0, true);
		}
	}

	private async void OnMenuClose(Menu menu, MenuClosedEventArgs e)
	{
		MoneyDisplayScript.ForceMoneyDisplay = false;
		Game.PlayerPed.Task.ClearAll();
		await BaseScript.Delay(4000);
		Game.PlayerPed.Task.ClearAllImmediately();
	}

	private async void RefreshMenu(long? wallet = null, long? balance = null)
	{
		if (!wallet.HasValue || !balance.HasValue)
		{
			atmMenu.ClearMenuItems();
			atmMenu.AddLoadingMenuItem();
		}
		if (!wallet.HasValue)
		{
			wallet = await Money.GetCachedBalanceOrFetch(AccountType.Cash);
		}
		if (!balance.HasValue)
		{
			balance = await Money.GetCachedBalanceOrFetch(AccountType.Bank);
		}
		List<string> list = new List<string>();
		withdrawAmounts.Clear();
		if (balance > 0 && balance > 3500)
		{
			long? num = balance - 3500;
			foreach (long atmAmount in atmAmounts)
			{
				if (atmAmount > num.Value)
				{
					if (num > 0)
					{
						withdrawAmounts.Add(num.Value);
						list.Add("~g~" + num.Value.ToCurrencyString());
					}
					break;
				}
				withdrawAmounts.Add(atmAmount);
				list.Add("~g~" + atmAmount.ToCurrencyString());
			}
			list.Add(LocalizationController.S(Entries.Banking.MENU_ATM_CUSTOM_AMOUNT));
			canWithdraw = true;
		}
		else
		{
			list.Add(LocalizationController.S(Entries.Banking.MENU_ATM_NO_FUNDS));
			canWithdraw = false;
		}
		List<string> list2 = new List<string>();
		depositAmounts.Clear();
		if (wallet > 0)
		{
			foreach (long atmAmount2 in atmAmounts)
			{
				if (atmAmount2 > wallet.Value)
				{
					depositAmounts.Add(wallet.Value);
					list2.Add("~g~" + wallet.Value.ToCurrencyString());
					break;
				}
				depositAmounts.Add(atmAmount2);
				list2.Add("~g~" + atmAmount2.ToCurrencyString());
			}
			list2.Add(LocalizationController.S(Entries.Banking.MENU_ATM_CUSTOM_AMOUNT));
			canDeposit = true;
		}
		else
		{
			list2.Add(LocalizationController.S(Entries.Banking.MENU_ATM_NO_FUNDS));
			canDeposit = false;
		}
		atmMenu.ClearMenuItems();
		Menu menu = atmMenu;
		MenuItem item = (menuItems["withdraw"] = new MenuListItem(LocalizationController.S(Entries.Banking.MENU_ATM_WITHDRAW_TEXT), list, 0)
		{
			Description = LocalizationController.S(Entries.Banking.MENU_ATM_WITHDRAW_DESCR, 3500.ToCurrencyString()),
			HideArrowsWhenNotSelected = true
		});
		menu.AddMenuItem(item);
		Menu menu2 = atmMenu;
		item = (menuItems["deposit"] = new MenuListItem(LocalizationController.S(Entries.Banking.MENU_ATM_DEPOSIT_TEXT), list2, 0)
		{
			Description = LocalizationController.S(Entries.Banking.MENU_ATM_DEPOSIT_DESCR, 3500.ToCurrencyString()),
			HideArrowsWhenNotSelected = true
		});
		menu2.AddMenuItem(item);
		atmMenu.RefreshIndex();
	}

	private void OnMenuListItemSelect(Menu menu, MenuListItem listItem, int selectedIndex, int itemIndex)
	{
		SelectMenuItem(menu, listItem);
	}

	private void OnMenuItemSelect(Menu menu, MenuItem menuItem, int itemIndex)
	{
		SelectMenuItem(menu, menuItem);
	}

	private async void SelectMenuItem(Menu menu, MenuItem menuItem)
	{
		if (isBusy || IsHacking)
		{
			return;
		}
		if (CuffedScript.IsBeingCuffedOrUncuffed || CuffedScript.IsCuffed || CuffedScript.IsInCustody)
		{
			Utils.DisplayHelpText(LocalizationController.S(Entries.Banking.ATM_CUFFED));
			return;
		}
		isBusy = true;
		try
		{
			if (IsSelected("withdraw"))
			{
				if (!canWithdraw)
				{
					Utils.DisplayHelpText(LocalizationController.S(Entries.Banking.ATM_INSUFFICIENT_FUNDS));
					return;
				}
				int listIndex = (menuItem as MenuListItem).ListIndex;
				long num;
				if (listIndex < withdrawAmounts.Count)
				{
					num = withdrawAmounts[listIndex];
				}
				else
				{
					if (!int.TryParse(await Utils.GetUserInput(LocalizationController.S(Entries.Banking.INPUT_ATM_WITHDRAW_TITLE), LocalizationController.S(Entries.Banking.INPUT_ATM_WITHDRAW_CONTENT), "", 11, "number"), out var result))
					{
						return;
					}
					if (result < 50000)
					{
						Utils.DisplayHelpText(LocalizationController.S(Entries.Banking.WITHDRAW_TOO_LITTLE, 50000.ToCurrencyString()));
						return;
					}
					if (result > 5000000)
					{
						Utils.DisplayHelpText(LocalizationController.S(Entries.Banking.WITHDRAW_TOO_MUCH, 5000000.ToCurrencyString()));
						return;
					}
					num = result;
				}
				if (num > 0)
				{
					switch ((UseATMResponse)(await TriggerServerEventAsync<int>("gtacnr:businesses:atm:withdraw", new object[2]
					{
						num,
						((Entity)closestAtmProp).NetworkId
					})))
					{
					case UseATMResponse.Success:
						RefreshMenu();
						break;
					case UseATMResponse.InsufficientFunds:
						Utils.DisplayHelpText(LocalizationController.S(Entries.Banking.ATM_INSUFFICIENT_FUNDS));
						break;
					case UseATMResponse.InvalidAmount:
						Utils.DisplayHelpText(LocalizationController.S(Entries.Banking.ATM_INVALID_AMOUNT));
						break;
					case UseATMResponse.CantCoverFees:
						Utils.DisplayHelpText(LocalizationController.S(Entries.Banking.ATM_CANT_PAY_FEE));
						break;
					default:
						Utils.DisplayHelpText(LocalizationController.S(Entries.Main.UNEXPECTED_ERROR));
						break;
					}
				}
			}
			else
			{
				if (!IsSelected("deposit"))
				{
					return;
				}
				if (!canDeposit)
				{
					Utils.DisplayHelpText(LocalizationController.S(Entries.Banking.ATM_INSUFFICIENT_FUNDS));
					return;
				}
				int listIndex2 = (menuItem as MenuListItem).ListIndex;
				long num2;
				if (listIndex2 < depositAmounts.Count)
				{
					num2 = depositAmounts[listIndex2];
				}
				else
				{
					if (!int.TryParse(await Utils.GetUserInput(LocalizationController.S(Entries.Banking.INPUT_ATM_DEPOSIT_TITLE), LocalizationController.S(Entries.Banking.INPUT_ATM_DEPOSIT_CONTENT), "", 11, "number"), out var result2))
					{
						return;
					}
					if (result2 < 50000)
					{
						Utils.DisplayHelpText(LocalizationController.S(Entries.Banking.DEPOSIT_TOO_LITTLE, 50000.ToCurrencyString()));
						return;
					}
					if (result2 > 5000000)
					{
						Utils.DisplayHelpText(LocalizationController.S(Entries.Banking.DEPOSIT_TOO_MUCH, 5000000.ToCurrencyString()));
						return;
					}
					num2 = result2;
				}
				if (num2 > 0)
				{
					switch ((UseATMResponse)(await TriggerServerEventAsync<int>("gtacnr:businesses:atm:deposit", new object[2]
					{
						num2,
						((Entity)closestAtmProp).NetworkId
					})))
					{
					case UseATMResponse.Success:
						RefreshMenu();
						break;
					case UseATMResponse.InsufficientFunds:
						Utils.DisplayHelpText(LocalizationController.S(Entries.Banking.ATM_INSUFFICIENT_FUNDS));
						break;
					case UseATMResponse.InvalidAmount:
						Utils.DisplayHelpText(LocalizationController.S(Entries.Banking.ATM_INVALID_AMOUNT));
						break;
					case UseATMResponse.CantCoverFees:
						Utils.DisplayHelpText(LocalizationController.S(Entries.Banking.ATM_CANT_PAY_FEE));
						break;
					default:
						Utils.DisplayHelpText(LocalizationController.S(Entries.Main.UNEXPECTED_ERROR));
						break;
					}
				}
			}
		}
		catch (Exception exception)
		{
			Print(exception);
		}
		finally
		{
			isBusy = false;
		}
		bool IsSelected(string key)
		{
			if (menuItems.ContainsKey(key))
			{
				return menuItem == menuItems[key];
			}
			return false;
		}
	}

	private async Task HackATM()
	{
		if ((Entity)(object)closestAtmProp == (Entity)null || isBusy || IsHacking || ((Entity)Game.PlayerPed).IsDead || Gtacnr.Client.API.Jobs.CachedJobEnum.IsPublicService())
		{
			return;
		}
		if (CuffedScript.IsBeingCuffedOrUncuffed || CuffedScript.IsCuffed || CuffedScript.IsInCustody)
		{
			Utils.DisplayHelpText(LocalizationController.S(Entries.Banking.ATM_CUFFED));
			return;
		}
		if (API.DecorGetInt(((PoolObject)closestAtmProp).Handle, "gtacnr:atmOutOfService") == 1)
		{
			Utils.DisplayHelpText(LocalizationController.S(Entries.Banking.ATM_OUT_OF_SERVICE));
			return;
		}
		IEnumerable<InventoryEntry> enumerable = InventoryMenuScript.Cache;
		if (enumerable == null)
		{
			enumerable = await InventoryMenuScript.ReloadInventory();
		}
		InventoryEntry inventoryEntry = enumerable.FirstOrDefault((InventoryEntry entry) => entry.ItemId == "atm_hack");
		if (inventoryEntry == null || inventoryEntry.Amount < 1f)
		{
			InventoryItem itemDefinition = Gtacnr.Data.Items.GetItemDefinition("atm_hack");
			Utils.DisplayHelpText(LocalizationController.S(Entries.Banking.ATM_NO_HACKING_DEVICE, itemDefinition.Name), playSound: false);
			Utils.PlayErrorSound();
			return;
		}
		if (!Gtacnr.Utils.CheckTimePassed(lastHackTimestamp, Constants.ATM.HACK_COOLDOWN - TimeSpan.FromSeconds(6.0)))
		{
			TimeSpan timeSpan = Constants.ATM.HACK_COOLDOWN - TimeSpan.FromSeconds(6.0) - (DateTime.UtcNow - lastHackTimestamp);
			Utils.DisplayHelpText(LocalizationController.S(Entries.Banking.ATM_HACKING_COOLDOWN, $"{timeSpan.TotalSeconds:0}"));
			Utils.PlayErrorSound();
			return;
		}
		isBusy = true;
		IsHacking = true;
		try
		{
			MenuController.CloseAllMenus();
			Game.PlayerPed.Task.ClearAllImmediately();
			((Entity)Game.PlayerPed).IsPositionFrozen = true;
			Game.PlayerPed.Weapons.Select((WeaponHash)(-1569615261), true);
			API.TaskTurnPedToFaceEntity(API.PlayerPedId(), ((PoolObject)closestAtmProp).Handle, 1000);
			await BaseScript.Delay(1000);
			Game.PlayerPed.Task.PlayAnimation("anim@heists@humane_labs@emp@hack_door", "hack_intro");
			await BaseScript.Delay(1000);
			Prop prop = await World.CreateProp(new Model("imp_prop_impexp_tablet"), ((Entity)Game.PlayerPed).Position, false, false);
			AntiEntitySpawnScript.RegisterEntity((Entity)(object)prop);
			API.AttachEntityToEntity(((PoolObject)prop).Handle, ((PoolObject)Game.PlayerPed).Handle, API.GetPedBoneIndex(((PoolObject)Game.PlayerPed).Handle, 57005), 0.17f, 0.05f, -0.019f, 247f, -175.5f, 0f, true, true, false, true, 1, true);
			await BaseScript.Delay(2000);
			Game.PlaySound("Beep_Green", "DLC_HEIST_HACKING_SNAKE_SOUNDS");
			await BaseScript.Delay(3000);
			Game.PlayerPed.Task.PlayAnimation("anim@heists@humane_labs@emp@hack_door", "hack_loop");
			HackATMResponse hackATMResponse = (HackATMResponse)(await TriggerServerEventAsync<int>("gtacnr:businesses:atm:hack", new object[1] { ((PoolObject)closestAtmProp).Handle }));
			switch (hackATMResponse)
			{
			case HackATMResponse.MissingToolkit:
			{
				InventoryItem itemDefinition2 = Gtacnr.Data.Items.GetItemDefinition("atm_hack");
				Utils.DisplayHelpText(LocalizationController.S(Entries.Banking.ATM_NO_HACKING_DEVICE, itemDefinition2.Name), playSound: false);
				Utils.PlayErrorSound();
				break;
			}
			case HackATMResponse.Cooldown:
				Utils.PlayErrorSound();
				break;
			default:
				Utils.DisplayErrorMessage(82, (int)hackATMResponse);
				break;
			case HackATMResponse.Success:
				Game.PlaySound("ROBBERY_MONEY_TOTAL", "HUD_FRONTEND_CUSTOM_SOUNDSET");
				API.DecorSetInt(((PoolObject)closestAtmProp).Handle, "gtacnr:atmOutOfService", 1);
				MenuController.CloseAllMenus();
				lastHackTimestamp = DateTime.UtcNow;
				break;
			}
			if (hackATMResponse == HackATMResponse.Success)
			{
				await BaseScript.Delay(5000);
			}
			Game.PlayerPed.Task.PlayAnimation("anim@heists@humane_labs@emp@hack_door", "hack_outro");
			await BaseScript.Delay(2000);
			((PoolObject)prop).Delete();
			await BaseScript.Delay(2000);
			Game.PlayerPed.Task.ClearAllImmediately();
			((Entity)Game.PlayerPed).IsPositionFrozen = false;
		}
		catch (Exception exception)
		{
			Print(exception);
		}
		finally
		{
			IsHacking = false;
			isBusy = false;
		}
	}

	private void EnableControls()
	{
		if (!controlsEnabled)
		{
			controlsEnabled = true;
			Utils.AddInstructionalButton("atmUse", new InstructionalButton(LocalizationController.S(Entries.Banking.BTN_ATM_USE), 2, 51));
			KeysScript.AttachListener((Control)51, OnKeyEvent, 10);
			if (!Gtacnr.Client.API.Jobs.CachedJobEnum.IsPublicService())
			{
				Utils.AddInstructionalButton("atmHack", new InstructionalButton(LocalizationController.S(Entries.Banking.BTN_ATM_HACK), 2, 29));
				KeysScript.AttachListener((Control)29, OnKeyEvent, 10);
			}
		}
	}

	private void DisableControls()
	{
		if (controlsEnabled)
		{
			controlsEnabled = false;
			Utils.RemoveInstructionalButton("atmUse");
			Utils.RemoveInstructionalButton("atmHack");
			KeysScript.DetachListener((Control)51, OnKeyEvent);
			KeysScript.DetachListener((Control)29, OnKeyEvent);
		}
	}

	private bool OnKeyEvent(Control control, KeyEventType eventType, InputType inputType)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0003: Invalid comparison between Unknown and I4
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Invalid comparison between Unknown and I4
		if ((int)control == 51 && eventType == KeyEventType.JustPressed)
		{
			OpenMenu();
			return true;
		}
		if ((int)control == 29 && eventType == KeyEventType.JustPressed)
		{
			HackATM();
			return true;
		}
		return false;
	}

	[Update]
	private async Coroutine FindTick()
	{
		await Script.Wait(1000);
		float num = 100f;
		Prop val = closestAtmProp;
		Vector3 position = ((Entity)Game.PlayerPed).Position;
		canUse = false;
		closestAtmProp = null;
		Prop[] allProps = World.GetAllProps();
		foreach (Prop val2 in allProps)
		{
			if (Constants.ATM.AtmPropHashes.Contains((uint)((Entity)val2).Model.Hash))
			{
				float num2 = ((Vector3)(ref position)).DistanceToSquared(((Entity)val2).Position);
				if (num2 < num * num)
				{
					closestAtmProp = val2;
					num = num2;
					canUse = num2 < 2f;
				}
			}
		}
		if ((Entity)(object)val != (Entity)(object)closestAtmProp)
		{
			if ((Entity)(object)val != (Entity)null && ((Entity)val).AttachedBlip != (Blip)null)
			{
				((PoolObject)((Entity)val).AttachedBlip).Delete();
			}
			if ((Entity)(object)closestAtmProp != (Entity)null && ((Entity)closestAtmProp).AttachedBlip == (Blip)null)
			{
				Blip obj = ((Entity)closestAtmProp).AttachBlip();
				obj.Sprite = (BlipSprite)276;
				obj.Color = (BlipColor)2;
				Utils.SetBlipName(obj, "ATM", "atm");
				obj.IsShortRange = true;
			}
		}
	}

	[Update]
	private async Coroutine UpdateTick()
	{
		if ((Entity)(object)closestAtmProp != (Entity)null && canUse)
		{
			if ((Entity)(object)closestAtmProp != (Entity)(object)prevClosestAtmProp)
			{
				prevClosestAtmProp = closestAtmProp;
				RefreshMenu();
			}
			if (canUse)
			{
				if (!atmMenu.Visible && !IsHacking && ((Entity)Game.PlayerPed).IsAlive)
				{
					EnableControls();
				}
			}
			else
			{
				DisableControls();
			}
		}
		else
		{
			DisableControls();
		}
	}
}
