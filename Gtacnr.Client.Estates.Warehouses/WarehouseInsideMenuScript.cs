using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CitizenFX.Core;
using Gtacnr.Client.API;
using Gtacnr.Client.IMenu;
using Gtacnr.Client.Jobs.Police.Arrest;
using Gtacnr.Data;
using Gtacnr.Localization;
using Gtacnr.Model;
using Gtacnr.Model.Enums;
using Gtacnr.ResponseCodes;
using MenuAPI;

namespace Gtacnr.Client.Estates.Warehouses;

public class WarehouseInsideMenuScript : Script
{
	private static WarehouseInsideMenuScript instance;

	private Menu menu;

	private Dictionary<string, Menu> submenus = new Dictionary<string, Menu>();

	private Dictionary<string, MenuItem> menuItems = new Dictionary<string, MenuItem>();

	private float totalWeight;

	private bool isBusy;

	private List<InventoryEntry> primaryInventory;

	private List<InventoryEntry> jobInventory;

	private List<InventoryEntry> warehouseInventory;

	public WarehouseInsideMenuScript()
	{
		instance = this;
	}

	protected override void OnStarted()
	{
		menu = new Menu("Warehouse", "Manage your warehouse")
		{
			MaxDistance = 7.5f
		};
		MenuController.AddMenu(menu);
	}

	public static void OpenMenu()
	{
		if (WarehouseScript.CurrentWarehouse != null)
		{
			if (!instance.menu.Visible)
			{
				instance.RefreshManageMenu();
			}
			if (WarehouseScript.CurrentWarehouse == null || CuffedScript.IsCuffed || CuffedScript.IsBeingCuffedOrUncuffed || Game.PlayerPed.IsBeingStunned)
			{
				Utils.PlayErrorSound();
			}
			else
			{
				instance.menu.OpenMenu();
			}
		}
	}

	private async void RefreshManageMenu()
	{
		menu.ClearMenuItems();
		menu.AddLoadingMenuItem();
		string jobId = Gtacnr.Client.API.Jobs.CachedJob;
		Job jobData = Gtacnr.Data.Jobs.GetJobData(jobId);
		primaryInventory = InventoryMenuScript.Cache?.ToList();
		if (primaryInventory == null)
		{
			primaryInventory = (await InventoryMenuScript.ReloadInventory()).ToList();
		}
		if (jobData.HasJobInventory)
		{
			jobInventory = await Inventories.GetJobInventory(jobId);
		}
		else
		{
			jobInventory = null;
		}
		warehouseInventory = await Inventories.GetStorageInventory(WarehouseScript.CurrentWarehouse.Item1);
		menu.ClearMenuItems();
		Menu obj = menu;
		MenuItem item = (menuItems["put"] = new MenuItem("Put items in", "Put items in your warehouse."));
		obj.AddMenuItem(item);
		Menu obj2 = menu;
		item = (menuItems["take"] = new MenuItem("Take items out", "Take items out of your warehouse."));
		obj2.AddMenuItem(item);
		submenus["put"] = new Menu("Warehouse", "Put items in warehouse")
		{
			MaxDistance = 7.5f
		};
		submenus["take"] = new Menu("Warehouse", "Take items out of warehouse")
		{
			MaxDistance = 7.5f
		};
		MenuController.BindMenuItem(menu, submenus["put"], menuItems["put"]);
		MenuController.BindMenuItem(menu, submenus["take"], menuItems["take"]);
		submenus["putPrimary"] = submenus["put"];
		if (jobInventory != null)
		{
			Menu obj3 = submenus["put"];
			Dictionary<string, MenuItem> dictionary = menuItems;
			MenuItem obj4 = new MenuItem("Inventory", "Put items from your inventory into your warehouse.")
			{
				LeftIcon = MenuItem.Icon.GTACNR_INVENTORY
			};
			item = obj4;
			dictionary["putPrimary"] = obj4;
			obj3.AddMenuItem(item);
			Menu obj5 = submenus["put"];
			Dictionary<string, MenuItem> dictionary2 = menuItems;
			MenuItem obj6 = new MenuItem("~b~Stock", "Put items from your ~b~stock menu ~s~into your warehouse.")
			{
				LeftIcon = MenuItem.Icon.GTACNR_STOCK
			};
			item = obj6;
			dictionary2["putJob"] = obj6;
			obj5.AddMenuItem(item);
			submenus["putPrimary"] = new Menu("Warehouse", "Inventory › Warehouse")
			{
				MaxDistance = 7.5f
			};
			submenus["putJob"] = new Menu("Warehouse", "Stock › Warehouse")
			{
				MaxDistance = 7.5f
			};
			MenuController.BindMenuItem(submenus["put"], submenus["putPrimary"], menuItems["putPrimary"]);
			MenuController.BindMenuItem(submenus["put"], submenus["putJob"], menuItems["putJob"]);
			foreach (InventoryEntry entry in from e in jobInventory
				where Gtacnr.Data.Items.IsItemDefined(e.ItemId) && e.Amount > 0f
				orderby Gtacnr.Data.Items.GetItemDefinition(e.ItemId).Category
				orderby e.Position
				select e)
			{
				float otherAmount = warehouseInventory.FirstOrDefault((InventoryEntry e) => e.ItemId == entry.ItemId)?.Amount ?? 0f;
				submenus["putJob"].AddMenuItem(GetMenuItemFromEntry(entry, otherAmount, "Inventory", "Stock"));
			}
			submenus["putJob"].PlaySelectSound = false;
			submenus["putJob"].InstructionalButtons.Clear();
			submenus["putJob"].InstructionalButtons.Add((Control)201, "Put");
			submenus["putJob"].InstructionalButtons.Add((Control)202, LocalizationController.S(Entries.Main.BTN_BACK));
			submenus["putJob"].OnItemSelect += OnMenuItemSelect;
		}
		foreach (InventoryEntry entry2 in from e in primaryInventory
			where Gtacnr.Data.Items.IsItemDefined(e.ItemId) && e.Amount > 0f
			orderby Gtacnr.Data.Items.GetItemDefinition(e.ItemId).Category
			orderby e.Position
			select e)
		{
			float otherAmount2 = warehouseInventory.FirstOrDefault((InventoryEntry e) => e.ItemId == entry2.ItemId)?.Amount ?? 0f;
			submenus["putPrimary"].AddMenuItem(GetMenuItemFromEntry(entry2, otherAmount2, "Inventory", "Warehouse"));
		}
		submenus["putPrimary"].PlaySelectSound = false;
		submenus["putPrimary"].InstructionalButtons.Clear();
		submenus["putPrimary"].InstructionalButtons.Add((Control)201, "Put");
		submenus["putPrimary"].InstructionalButtons.Add((Control)202, LocalizationController.S(Entries.Main.BTN_BACK));
		submenus["putPrimary"].OnItemSelect += OnMenuItemSelect;
		submenus["takePrimary"] = submenus["take"];
		if (jobInventory != null)
		{
			Menu obj7 = submenus["take"];
			Dictionary<string, MenuItem> dictionary3 = menuItems;
			MenuItem obj8 = new MenuItem("Inventory", "Put warehouse items into your inventory.")
			{
				LeftIcon = MenuItem.Icon.GTACNR_INVENTORY
			};
			item = obj8;
			dictionary3["takePrimary"] = obj8;
			obj7.AddMenuItem(item);
			Menu obj9 = submenus["take"];
			Dictionary<string, MenuItem> dictionary4 = menuItems;
			MenuItem obj10 = new MenuItem("~b~Stock", "Put warehouse items into your ~b~stock menu~s~.~n~~y~Warning: ~s~only compatible items will be shown.")
			{
				LeftIcon = MenuItem.Icon.GTACNR_STOCK
			};
			item = obj10;
			dictionary4["takeJob"] = obj10;
			obj9.AddMenuItem(item);
			submenus["takePrimary"] = new Menu("Warehouse", "Warehouse › Inventory")
			{
				MaxDistance = 7.5f
			};
			submenus["takeJob"] = new Menu("Warehouse", "Warehouse › Stock")
			{
				MaxDistance = 7.5f
			};
			MenuController.BindMenuItem(submenus["take"], submenus["takePrimary"], menuItems["takePrimary"]);
			MenuController.BindMenuItem(submenus["take"], submenus["takeJob"], menuItems["takeJob"]);
			foreach (InventoryEntry entry3 in from e in warehouseInventory
				where Gtacnr.Data.Items.IsItemDefined(e.ItemId) && e.Amount > 0f && jobData.StockItems.Contains(e.ItemId)
				orderby Gtacnr.Data.Items.GetItemDefinition(e.ItemId).Category
				orderby e.Position
				select e)
			{
				float otherAmount3 = jobInventory.FirstOrDefault((InventoryEntry e) => e.ItemId == entry3.ItemId)?.Amount ?? 0f;
				submenus["takeJob"].AddMenuItem(GetMenuItemFromEntry(entry3, otherAmount3, "Warehouse", "Stock"));
			}
			submenus["takeJob"].PlaySelectSound = false;
			submenus["takeJob"].InstructionalButtons.Clear();
			submenus["takeJob"].InstructionalButtons.Add((Control)201, "Take");
			submenus["takeJob"].InstructionalButtons.Add((Control)202, LocalizationController.S(Entries.Main.BTN_BACK));
			submenus["takeJob"].OnItemSelect += OnMenuItemSelect;
		}
		totalWeight = 0f;
		foreach (InventoryEntry entry4 in from e in warehouseInventory
			where Gtacnr.Data.Items.IsItemDefined(e.ItemId) && e.Amount > 0f
			orderby Gtacnr.Data.Items.GetItemDefinition(e.ItemId).Category
			orderby e.Position
			select e)
		{
			float otherAmount4 = primaryInventory.FirstOrDefault((InventoryEntry e) => e.ItemId == entry4.ItemId)?.Amount ?? 0f;
			submenus["takePrimary"].AddMenuItem(GetMenuItemFromEntry(entry4, otherAmount4, "Warehouse", "Inventory"));
			InventoryItem itemDefinition = Gtacnr.Data.Items.GetItemDefinition(entry4.ItemId);
			totalWeight += itemDefinition.Weight * entry4.Amount;
		}
		submenus["takePrimary"].PlaySelectSound = false;
		submenus["takePrimary"].InstructionalButtons.Clear();
		submenus["takePrimary"].InstructionalButtons.Add((Control)201, "Take");
		submenus["takePrimary"].InstructionalButtons.Add((Control)202, LocalizationController.S(Entries.Main.BTN_BACK));
		submenus["takePrimary"].OnItemSelect += OnMenuItemSelect;
		RefreshMenuWeightText();
	}

	private void OnMenuItemSelect(Menu menu, MenuItem menuItem, int itemIndex)
	{
		if (WarehouseScript.CurrentWarehouse == null || CuffedScript.IsCuffed || CuffedScript.IsBeingCuffedOrUncuffed || Game.PlayerPed.IsBeingStunned)
		{
			menu.CloseMenu();
			Utils.PlayErrorSound();
		}
		else if (IsMenu("putPrimary"))
		{
			OnPut(submenus["putPrimary"]);
		}
		else if (IsMenu("putJob"))
		{
			OnPut(submenus["putJob"]);
		}
		else if (IsMenu("takePrimary"))
		{
			OnTake(submenus["takePrimary"]);
		}
		else if (IsMenu("takeJob"))
		{
			OnTake(submenus["takeJob"]);
		}
		bool IsMenu(string key)
		{
			if (submenus.ContainsKey(key))
			{
				return submenus[key] == menu;
			}
			return false;
		}
	}

	private async void OnPut(Menu menu)
	{
		MenuItem currentMenuItem = menu.GetCurrentMenuItem();
		InventoryEntry entry = currentMenuItem.ItemData as InventoryEntry;
		InventoryType type = InventoryType.Invalid;
		if (!Gtacnr.Data.Items.GetItemDefinition(entry.ItemId).CanMove)
		{
			Utils.PlayErrorSound();
			Utils.SendNotification("You cannot ~r~move ~s~this item into a warehouse.");
			return;
		}
		if (Selected("putPrimary"))
		{
			type = InventoryType.Primary;
		}
		else if (Selected("putJob"))
		{
			type = InventoryType.Job;
		}
		if (float.TryParse(await Utils.GetUserInput("Amount", "Enter the amount to put in the warehouse.", "", 11, "number"), out var result))
		{
			await PutIntoWarehouse(type, entry, result);
		}
		else
		{
			Utils.PlayErrorSound();
		}
		bool Selected(string menuId)
		{
			if (submenus.ContainsKey(menuId))
			{
				return menu == submenus[menuId];
			}
			return false;
		}
	}

	private async Task PutIntoWarehouse(InventoryType type, InventoryEntry entry, float amount)
	{
		if (isBusy || amount <= 0f)
		{
			return;
		}
		isBusy = true;
		try
		{
			UseStorageResponse useStorageResponse = (UseStorageResponse)(await TriggerServerEventAsync<int>("gtacnr:warehouses:put", new object[3]
			{
				(int)type,
				entry.ItemId,
				amount
			}));
			switch (useStorageResponse)
			{
			case UseStorageResponse.InsufficientAmount:
				Utils.DisplayHelpText("~r~You don't have that amount of this item!", playSound: false);
				goto IL_0189;
			case UseStorageResponse.LimitReached:
				Utils.DisplayHelpText("~r~You can't hold that many of this item!", playSound: false);
				goto IL_0189;
			case UseStorageResponse.NoSpaceLeft:
				Utils.DisplayHelpText("~r~There's not enough space left in the warehouse!", playSound: false);
				goto IL_0189;
			case UseStorageResponse.JobNotAllowed:
				Utils.DisplayHelpText("~r~This item cannot be taken from your " + Gtacnr.Data.Jobs.GetJobData(Gtacnr.Client.API.Jobs.CachedJob).Name + " stock inventory!", playSound: false);
				goto IL_0189;
			default:
				Utils.DisplayHelpText(LocalizationController.S(Entries.Main.UNEXPECTED_ERROR_CODE, $"0x57-{(int)useStorageResponse}"), playSound: false);
				goto IL_0189;
			case UseStorageResponse.Success:
				{
					InventoryItem itemDefinition = Gtacnr.Data.Items.GetItemDefinition(entry.ItemId);
					string text = ((amount == 1f) ? "a" : $"~y~{amount:0.##}");
					totalWeight += amount * itemDefinition.Weight;
					InventoryEntry inventoryEntry = warehouseInventory.FirstOrDefault((InventoryEntry e) => e.ItemId == entry.ItemId);
					UpdatePutEntryMenuItem(type, entry, inventoryEntry?.Amount ?? 0f);
					UpdatePutEntryTakeMenuItem(entry.ItemId, entry.Amount, amount);
					RefreshMenuWeightText();
					Utils.SendNotification("You put " + text + " ~p~" + itemDefinition.Name + " ~s~in your warehouse.");
					Utils.PlaySelectSound();
					break;
				}
				IL_0189:
				Utils.PlayErrorSound();
				break;
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
	}

	private void UpdatePutEntryMenuItem(InventoryType inventoryType, InventoryEntry entry, float otherAmount)
	{
		Menu menu = null;
		switch (inventoryType)
		{
		case InventoryType.Primary:
			menu = submenus["putPrimary"];
			break;
		case InventoryType.Job:
			menu = submenus["putJob"];
			break;
		}
		if (menu == null)
		{
			return;
		}
		MenuItem menuItem = (from i in menu.GetMenuItems()
			where i.ItemData is InventoryEntry inventoryEntry && inventoryEntry.ItemId == entry.ItemId
			select i).FirstOrDefault();
		if (menuItem != null)
		{
			InventoryItem itemDefinition = Gtacnr.Data.Items.GetItemDefinition(entry.ItemId);
			if (entry.Amount > 0f)
			{
				menuItem.Label = $"{entry.Amount:0.##}{itemDefinition.Unit}";
			}
			else
			{
				menu.RemoveMenuItem(menuItem);
			}
		}
	}

	private void UpdatePutEntryTakeMenuItem(string itemId, float otherAmount, float amountAdded)
	{
		foreach (string item in new List<string> { "takePrimary", "takeJob" })
		{
			if (submenus.ContainsKey(item) && submenus[item] != null)
			{
				Menu menu = submenus[item];
				MenuItem menuItem = (from i in menu.GetMenuItems()
					where i.ItemData is InventoryEntry inventoryEntry2 && inventoryEntry2.ItemId == itemId
					select i).FirstOrDefault();
				InventoryItem itemDefinition = Gtacnr.Data.Items.GetItemDefinition(itemId);
				if (menuItem == null)
				{
					menuItem = new MenuItem(itemDefinition.Name, itemDefinition.Description)
					{
						ItemData = new InventoryEntry
						{
							ItemId = itemId,
							Amount = amountAdded
						}
					};
					menu.AddMenuItem(menuItem);
				}
				InventoryEntry inventoryEntry = menuItem.ItemData as InventoryEntry;
				menuItem.Label = $"{inventoryEntry.Amount:0.##}{itemDefinition.Unit}";
			}
		}
	}

	private async void OnTake(Menu menu)
	{
		MenuItem currentMenuItem = menu.GetCurrentMenuItem();
		InventoryEntry entry = currentMenuItem.ItemData as InventoryEntry;
		InventoryType type = InventoryType.Invalid;
		if (!Gtacnr.Data.Items.GetItemDefinition(entry.ItemId).CanMove)
		{
			Utils.PlayErrorSound();
			Utils.SendNotification("You cannot ~r~move ~s~this item out of your warehouse.");
			return;
		}
		if (Selected("takePrimary"))
		{
			type = InventoryType.Primary;
		}
		else if (Selected("takeJob"))
		{
			type = InventoryType.Job;
		}
		if (float.TryParse(await Utils.GetUserInput("Amount", "Enter the amount to take from the warehouse.", "", 11, "number"), out var result))
		{
			await TakeFromWarehouse(type, entry, result);
		}
		else
		{
			Utils.PlayErrorSound();
		}
		bool Selected(string menuId)
		{
			if (submenus.ContainsKey(menuId))
			{
				return menu == submenus[menuId];
			}
			return false;
		}
	}

	private async Task TakeFromWarehouse(InventoryType type, InventoryEntry entry, float amount)
	{
		if (isBusy || amount <= 0f)
		{
			return;
		}
		isBusy = true;
		try
		{
			UseStorageResponse useStorageResponse = (UseStorageResponse)(await TriggerServerEventAsync<int>("gtacnr:warehouses:take", new object[3]
			{
				(int)type,
				entry.ItemId,
				amount
			}));
			switch (useStorageResponse)
			{
			case UseStorageResponse.InsufficientAmount:
				Utils.DisplayHelpText("~r~You don't have that amount of this item!", playSound: false);
				goto IL_0194;
			case UseStorageResponse.LimitReached:
				Utils.DisplayHelpText("~r~You can't hold that many of this item!", playSound: false);
				goto IL_0194;
			case UseStorageResponse.NoSpaceLeft:
				Utils.DisplayHelpText(LocalizationController.S(Entries.Main.NOT_ENOUGH_INVENTORY_SPACE), playSound: false);
				goto IL_0194;
			case UseStorageResponse.JobNotAllowed:
				Utils.DisplayHelpText("~r~This item cannot be placed in your " + Gtacnr.Data.Jobs.GetJobData(Gtacnr.Client.API.Jobs.CachedJob).Name + " stock inventory!", playSound: false);
				goto IL_0194;
			default:
				Utils.DisplayHelpText(LocalizationController.S(Entries.Main.UNEXPECTED_ERROR_CODE, $"0x58-{(int)useStorageResponse}"), playSound: false);
				goto IL_0194;
			case UseStorageResponse.Success:
				{
					InventoryItem itemDefinition = Gtacnr.Data.Items.GetItemDefinition(entry.ItemId);
					string text = ((amount == 1f) ? "a" : $"~y~{amount:0.##}");
					totalWeight += amount * itemDefinition.Weight;
					entry.Amount -= amount;
					InventoryEntry inventoryEntry = null;
					switch (type)
					{
					case InventoryType.Primary:
						inventoryEntry = primaryInventory.FirstOrDefault((InventoryEntry e) => e.ItemId == entry.ItemId);
						break;
					case InventoryType.Job:
						inventoryEntry = primaryInventory.FirstOrDefault((InventoryEntry e) => e.ItemId == entry.ItemId);
						break;
					}
					UpdateTakenEntryMenuItem(type, entry, inventoryEntry?.Amount ?? 0f);
					UpdateTakenEntryPutMenuItem(entry.ItemId, entry.Amount, amount);
					RefreshMenuWeightText();
					Utils.PlaySelectSound();
					Utils.SendNotification("You took " + text + " ~p~" + itemDefinition.Name + " ~s~from your warehouse.");
					break;
				}
				IL_0194:
				Utils.PlayErrorSound();
				break;
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
	}

	private void UpdateTakenEntryMenuItem(InventoryType inventoryType, InventoryEntry entry, float otherAmount)
	{
		Menu menu = null;
		switch (inventoryType)
		{
		case InventoryType.Primary:
			menu = submenus["takePrimary"];
			break;
		case InventoryType.Job:
			menu = submenus["takeJob"];
			break;
		}
		if (menu == null)
		{
			return;
		}
		MenuItem menuItem = (from i in menu.GetMenuItems()
			where i.ItemData is InventoryEntry inventoryEntry && inventoryEntry.ItemId == entry.ItemId
			select i).FirstOrDefault();
		if (menuItem != null)
		{
			InventoryItem itemDefinition = Gtacnr.Data.Items.GetItemDefinition(entry.ItemId);
			if (entry.Amount > 0f)
			{
				menuItem.Label = $"{entry.Amount:0.##}{itemDefinition.Unit}";
			}
			else
			{
				menu.RemoveMenuItem(menuItem);
			}
		}
	}

	private void UpdateTakenEntryPutMenuItem(string itemId, float otherAmount, float amountAdded)
	{
		foreach (string item in new List<string> { "putPrimary", "putJob" })
		{
			if (submenus.ContainsKey(item) && submenus[item] != null)
			{
				Menu menu = submenus[item];
				MenuItem menuItem = (from i in menu.GetMenuItems()
					where i.ItemData is InventoryEntry inventoryEntry2 && inventoryEntry2.ItemId == itemId
					select i).FirstOrDefault();
				InventoryItem itemDefinition = Gtacnr.Data.Items.GetItemDefinition(itemId);
				if (menuItem == null)
				{
					menuItem = new MenuItem(itemDefinition.Name, itemDefinition.Description)
					{
						ItemData = new InventoryEntry
						{
							ItemId = itemId,
							Amount = amountAdded
						}
					};
					menu.AddMenuItem(menuItem);
				}
				InventoryEntry inventoryEntry = menuItem.ItemData as InventoryEntry;
				menuItem.Label = $"{inventoryEntry.Amount:0.##}{itemDefinition.Unit}";
			}
		}
	}

	private MenuItem GetMenuItemFromEntry(InventoryEntry entry, float otherAmount, string currentLabel, string otherLabel)
	{
		InventoryItem itemDefinition = Gtacnr.Data.Items.GetItemDefinition(entry.ItemId);
		return new MenuItem(itemDefinition.Name)
		{
			Description = GetMenuItemDescriptionFromEntry(entry, otherAmount, currentLabel, otherLabel),
			Label = $"{entry.Amount:0.##}{itemDefinition.Unit}",
			ItemData = entry
		};
	}

	private string GetMenuItemDescriptionFromEntry(InventoryEntry entry, float otherAmount, string currentLabel, string otherLabel)
	{
		InventoryItem itemDefinition = Gtacnr.Data.Items.GetItemDefinition(entry.ItemId);
		return string.Format("{0} ({1:0.##}{2}) › {3} ({4:0.##}{5}){6}", currentLabel, entry.Amount, itemDefinition.Unit, otherLabel, otherAmount, itemDefinition.Unit, (!string.IsNullOrWhiteSpace(itemDefinition.Description)) ? ("\n" + itemDefinition.Description) : "");
	}

	private void RefreshMenuWeightText()
	{
		if (WarehouseScript.CurrentWarehouse != null)
		{
			Warehouse warehouse = WarehouseScript.Warehouses[WarehouseScript.CurrentWarehouse.Item1];
			float num = WarehouseScript.WarehouseInteriors[warehouse.InteriorId].Capacity / 1000f;
			float num2 = totalWeight / 1000f;
			string counterPreText = $"{num2:0.##}kg / {num:0.##}kg";
			menu.CounterPreText = counterPreText;
		}
	}
}
