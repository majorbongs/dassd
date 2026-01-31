using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CitizenFX.Core;
using Gtacnr.Client.API;
using Gtacnr.Client.Jobs.Police.Arrest;
using Gtacnr.Localization;
using Gtacnr.Model;
using Gtacnr.Model.Enums;
using Gtacnr.ResponseCodes;
using MenuAPI;

namespace Gtacnr.Client.Estates.Warehouses;

public class WarehouseMenuScript : Script
{
	private static WarehouseMenuScript instance;

	private Menu mainMenu;

	private Dictionary<string, MenuItem> menuItems = new Dictionary<string, MenuItem>();

	private Menu playersMenu;

	private EstatePlayerAction? selectedPlayerAction;

	private bool isBusy;

	private const string NOT_CONNECTED_STR = "~r~The owner of the warehouse is no longer connected. Please, reopen the menu to refresh the player list.";

	public WarehouseMenuScript()
	{
		instance = this;
	}

	protected override void OnStarted()
	{
		mainMenu = new Menu(LocalizationController.S(Entries.Properties.WAREHOUSE), LocalizationController.S(Entries.Properties.WAREHOUSE))
		{
			MaxDistance = 7.5f
		};
		MenuController.AddMenu(mainMenu);
		mainMenu.OnItemSelect += OnItemSelected;
		mainMenu.OnMenuOpen += OnMenuOpen;
		playersMenu = new Menu(LocalizationController.S(Entries.Properties.WAREHOUSE), LocalizationController.S(Entries.Properties.MENU_PROPERTY_OWNERS))
		{
			MaxDistance = 7.5f
		};
		MenuController.AddMenu(playersMenu);
		playersMenu.OnItemSelect += OnPlayerSelected;
		playersMenu.OnMenuOpen += OnMenuOpen;
	}

	public static void ShowMenu()
	{
		if (WarehouseScript.ClosestWarehouse != null)
		{
			if (!instance.mainMenu.Visible)
			{
				instance.RefreshWarehouseMenu();
			}
			instance.mainMenu.Visible = true;
		}
	}

	private async void RefreshWarehouseMenu()
	{
		Warehouse warehouse = WarehouseScript.ClosestWarehouse;
		if (warehouse == null)
		{
			mainMenu.Visible = false;
			return;
		}
		mainMenu.ClearMenuItems();
		mainMenu.MenuSubtitle = warehouse.Name;
		MenuItem item;
		if (!WarehouseScript.OwnedWarehouseIds.Contains(warehouse.Id))
		{
			string text = ((await Money.GetCachedBalanceOrFetch(AccountType.Bank) >= warehouse.Value) ? "~g~" : "~r~");
			Menu menu = mainMenu;
			item = (menuItems["buy"] = new MenuItem("Purchase")
			{
				Description = "Warehouses allow you to safely store items and drugs. You can ~r~not ~s~store vehicles, weapons or cash here!~s~~n~Size: " + WarehouseScript.GetWarehouseSize(warehouse) + "~s~~n~~b~" + LocalizationController.S(Entries.Businesses.ITEM_ATTRIBUTE_UNLOCK_LEVEL, warehouse.RequiredLevel) + "~s~",
				Label = text + warehouse.Value.ToCurrencyString()
			});
			menu.AddMenuItem(item);
		}
		else
		{
			Menu menu2 = mainMenu;
			item = (menuItems["enter"] = new MenuItem("Enter", "Enter your ~b~warehouse~s~."));
			menu2.AddMenuItem(item);
			Menu menu3 = mainMenu;
			Dictionary<string, MenuItem> dictionary = menuItems;
			MenuItem obj = new MenuItem("Sell", "Sell your ~b~warehouse ~s~to the server.\n~r~Notice: this feature has been temporarily disabled due to a bug.")
			{
				Label = "~g~" + ((double)warehouse.Value * 0.6).ToLong().ToCurrencyString(),
				Enabled = false
			};
			item = obj;
			dictionary["sell"] = obj;
			menu3.AddMenuItem(item);
		}
		Menu menu4 = mainMenu;
		item = (menuItems["knock"] = new MenuItem("~y~Knock")
		{
			Description = "Ask an owner of this warehouse to let you in.",
			Label = "›"
		});
		menu4.AddMenuItem(item);
		MenuController.BindMenuItem(mainMenu, playersMenu, menuItems["knock"]);
		if (Gtacnr.Client.API.Jobs.CachedJobEnum.IsPolice())
		{
			Menu menu5 = mainMenu;
			Dictionary<string, MenuItem> dictionary2 = menuItems;
			MenuItem obj2 = new MenuItem("~b~Raid")
			{
				Description = "Enter the warehouse forcibly if you saw a ~o~wanted suspect ~s~enter.",
				Label = "›"
			};
			item = obj2;
			dictionary2["forceEntry"] = obj2;
			menu5.AddMenuItem(item);
			MenuController.BindMenuItem(mainMenu, playersMenu, menuItems["forceEntry"]);
		}
		else if (!Gtacnr.Client.API.Jobs.CachedJobEnum.IsPublicService())
		{
			Menu menu6 = mainMenu;
			Dictionary<string, MenuItem> dictionary3 = menuItems;
			MenuItem obj3 = new MenuItem("~o~Break in")
			{
				Enabled = !Gtacnr.Client.API.Jobs.CachedJobEnum.IsPublicService(),
				Description = "Break into this warehouse.",
				Label = "›"
			};
			item = obj3;
			dictionary3["breakIn"] = obj3;
			menu6.AddMenuItem(item);
			MenuController.BindMenuItem(mainMenu, playersMenu, menuItems["breakIn"]);
		}
		mainMenu.AddMenuItem(new MenuItem("Information")
		{
			Description = "Size: " + WarehouseScript.GetWarehouseSize(warehouse) + "~s~~n~~b~" + LocalizationController.S(Entries.Businesses.ITEM_ATTRIBUTE_UNLOCK_LEVEL, warehouse.RequiredLevel) + "~s~~n~~g~Owned",
			PlaySelectSound = false
		});
		Menu menu7 = mainMenu;
		item = (menuItems["players"] = new MenuItem("Owners", "A list of connected players that own this garage.")
		{
			Label = "›"
		});
		menu7.AddMenuItem(item);
		MenuController.BindMenuItem(mainMenu, playersMenu, menuItems["players"]);
	}

	private async void RefreshPlayersMenu()
	{
		playersMenu.ClearMenuItems();
		playersMenu.AddLoadingMenuItem();
		List<int> list = (await TriggerServerEventAsync<string>("gtacnr:warehouses:getOnlineWarehouseOwners", new object[1] { WarehouseScript.ClosestWarehouse.Id })).Unjson<List<int>>();
		playersMenu.ClearMenuItems();
		foreach (int item2 in list)
		{
			if (item2 != Game.Player.ServerId)
			{
				PlayerState playerState = LatentPlayers.Get(item2);
				MenuItem item = new MenuItem($"{playerState.ColorTextCode}{playerState.Name} ({playerState.Id})")
				{
					ItemData = item2
				};
				playersMenu.AddMenuItem(item);
			}
		}
		if (playersMenu.GetMenuItems().Count == 0)
		{
			playersMenu.AddMenuItem(new MenuItem("No players :(", "There are no online players that own this warehouse."));
		}
	}

	private void OnMenuOpen(Menu menu)
	{
		if (menu == playersMenu)
		{
			RefreshPlayersMenu();
		}
	}

	private async void OnItemSelected(Menu menu, MenuItem menuItem, int itemIndex)
	{
		if (isBusy)
		{
			return;
		}
		isBusy = true;
		try
		{
			Warehouse warehouse = WarehouseScript.ClosestWarehouse;
			if (warehouse == null)
			{
				isBusy = false;
				MenuController.CloseAllMenus();
			}
			else if (Selected("buy"))
			{
				if (await Money.GetCachedBalanceOrFetch(AccountType.Bank) < warehouse.Value)
				{
					Utils.DisplayHelpText(LocalizationController.S(Entries.Main.NOT_ENOUGH_MONEY_BANK_ACCOUNT));
					isBusy = false;
				}
				else if (await Utils.ShowConfirm("Do you really want to purchase ~y~" + warehouse.Name + " ~s~for ~g~" + warehouse.Value.ToCurrencyString() + "~s~?"))
				{
					BuyPropertyResponse resp = (BuyPropertyResponse)(await TriggerServerEventAsync<int>("gtacnr:warehouses:buy", new object[1] { warehouse.Id }));
					await BaseScript.Delay(500);
					switch (resp)
					{
					case BuyPropertyResponse.Success:
						Utils.DisplayHelpText("You purchased ~y~" + warehouse.Name + " ~s~for ~g~" + warehouse.Value.ToCurrencyString() + "~s~.");
						WarehouseScript.OwnedWarehouseIds.Add(warehouse.Id);
						WarehouseScript.RefreshBlips();
						RefreshWarehouseMenu();
						break;
					case BuyPropertyResponse.Level:
						Utils.DisplayHelpText("~r~You don't have the required level to purchase this warehouse.", playSound: false);
						Utils.PlayErrorSound();
						break;
					case BuyPropertyResponse.WantedLevel:
						Utils.DisplayHelpText("~r~You are wanted by the police. Come back when you're clean!", playSound: false);
						Utils.PlayErrorSound();
						break;
					default:
						Utils.DisplayHelpText(LocalizationController.S(Entries.Main.UNEXPECTED_ERROR_CODE, $"0xC7-{(int)resp}"));
						break;
					}
				}
			}
			else if (Selected("enter"))
			{
				WarehouseScript.EnterWarehouse(warehouse, Game.Player.ServerId);
			}
			else if (Selected("sell"))
			{
				long sellPrice = ((double)warehouse.Value * 0.6).ToLong();
				if (await Utils.ShowConfirm("Do you really want to sell ~y~" + warehouse.Name + " ~s~for ~g~" + sellPrice.ToCurrencyString() + "~s~?\nIf you have any item inside, you will need to purchase the warehouse again to get them back."))
				{
					SellPropertyResponse sellPropertyResponse = (SellPropertyResponse)(await TriggerServerEventAsync<int>("gtacnr:warehouses:sell", new object[1] { warehouse.Id }));
					if (sellPropertyResponse == SellPropertyResponse.Success)
					{
						Utils.DisplayHelpText("You sold ~y~" + warehouse.Name + " ~s~for ~g~" + sellPrice.ToCurrencyString() + "~s~.");
						WarehouseScript.OwnedWarehouseIds.Remove(warehouse.Id);
						WarehouseScript.RefreshBlips();
						RefreshWarehouseMenu();
					}
					else
					{
						Utils.DisplayHelpText(LocalizationController.S(Entries.Main.UNEXPECTED_ERROR_CODE, $"0xC7-{(int)sellPropertyResponse}"));
					}
				}
			}
			else if (Selected("knock"))
			{
				selectedPlayerAction = EstatePlayerAction.Knock;
			}
			else if (Selected("forceEntry"))
			{
				selectedPlayerAction = EstatePlayerAction.ForceEntry;
			}
			else if (Selected("breakIn"))
			{
				selectedPlayerAction = EstatePlayerAction.BreakIn;
			}
			else if (Selected("players"))
			{
				selectedPlayerAction = null;
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
		bool Selected(string menuItemKey)
		{
			if (menuItems.ContainsKey(menuItemKey))
			{
				return menuItem == menuItems[menuItemKey];
			}
			return false;
		}
	}

	private async void OnPlayerSelected(Menu menu, MenuItem menuItem, int itemIndex)
	{
		if (isBusy || !selectedPlayerAction.HasValue)
		{
			return;
		}
		int num = (int)menuItem.ItemData;
		if (LatentPlayers.Get(num) == null)
		{
			Utils.DisplayHelpText("~r~The owner of the warehouse is no longer connected. Please, reopen the menu to refresh the player list.");
			return;
		}
		isBusy = true;
		try
		{
			Warehouse closestWarehouse = WarehouseScript.ClosestWarehouse;
			if (closestWarehouse == null)
			{
				isBusy = false;
				MenuController.CloseAllMenus();
				return;
			}
			EstatePlayerAction? estatePlayerAction = selectedPlayerAction;
			if (estatePlayerAction.HasValue)
			{
				switch (estatePlayerAction.GetValueOrDefault())
				{
				case EstatePlayerAction.Knock:
					MenuController.CloseAllMenus();
					await Knock(closestWarehouse, num);
					break;
				case EstatePlayerAction.ForceEntry:
					MenuController.CloseAllMenus();
					await ForceEntry(closestWarehouse, num);
					break;
				case EstatePlayerAction.BreakIn:
					MenuController.CloseAllMenus();
					await BreakInEntry(closestWarehouse, num);
					break;
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
	}

	private async Task Knock(Warehouse warehouse, int targetId)
	{
		PlayerState targetInfo = LatentPlayers.Get(targetId);
		KnockAtPropertyResponse knockAtPropertyResponse = (KnockAtPropertyResponse)(await TriggerServerEventAsync<int>("gtacnr:warehouses:knock", new object[2] { warehouse.Id, targetId }));
		switch (knockAtPropertyResponse)
		{
		case KnockAtPropertyResponse.Success:
			Utils.DisplayHelpText("You knocked at " + targetInfo.ColorNameAndId + "'s warehouse. Wait for them to answer.");
			break;
		case KnockAtPropertyResponse.Cooldown:
			Utils.DisplayHelpText("~r~You must wait before knocking again.");
			break;
		case KnockAtPropertyResponse.OwnerOffline:
			Utils.DisplayHelpText("~r~The owner of the warehouse is no longer connected. Please, reopen the menu to refresh the player list.");
			break;
		default:
			Utils.DisplayErrorMessage(100, (int)knockAtPropertyResponse);
			break;
		}
	}

	private async Task ForceEntry(Warehouse warehouse, int targetId)
	{
		PlayerState targetInfo = LatentPlayers.Get(targetId);
		BaseScript.TriggerEvent("dpemotes:cancelEmoteImmediately", new object[0]);
		Utils.RemoveAllAttachedProps();
		ForceEntryResponse forceEntryResponse = (ForceEntryResponse)(await TriggerServerEventAsync<int>("gtacnr:warehouses:forceEntry", new object[2] { warehouse.Id, targetId }));
		switch (forceEntryResponse)
		{
		case ForceEntryResponse.Success:
			WarehouseScript.EnterWarehouse(warehouse, targetId, skipAuth: true);
			Utils.DisplayHelpText("You ~b~entered " + targetInfo.ColorNameAndId + "'s warehouse.");
			break;
		case ForceEntryResponse.NoWarrant:
			Utils.DisplayHelpText("You don't have a ~o~warrant ~s~to enter this warehouse. You can only enter when you spot a ~o~wanted suspect ~s~enter.");
			break;
		case ForceEntryResponse.Cooldown:
			Utils.DisplayHelpText("~r~You must wait before attempting to enter again.");
			break;
		case ForceEntryResponse.OwnerOffline:
			Utils.DisplayHelpText("~r~The owner of the warehouse is no longer connected. Please, reopen the menu to refresh the player list.");
			break;
		default:
			Utils.DisplayErrorMessage(101, (int)forceEntryResponse);
			break;
		}
	}

	private async Task BreakInEntry(Warehouse warehouse, int targetId)
	{
		if (CuffedScript.IsCuffed || SurrenderScript.IsSurrendered || Game.PlayerPed.IsBeingStunned || CuffedScript.IsBeingCuffedOrUncuffed)
		{
			Utils.PlayErrorSound();
			return;
		}
		PlayerState targetInfo = LatentPlayers.Get(targetId);
		BaseScript.TriggerEvent("dpemotes:cancelEmoteImmediately", new object[0]);
		Utils.RemoveAllAttachedProps();
		ForceEntryResponse forceEntryResponse = (ForceEntryResponse)(await TriggerServerEventAsync<int>("gtacnr:warehouses:breakIn", new object[2] { warehouse.Id, targetId }));
		switch (forceEntryResponse)
		{
		case ForceEntryResponse.Success:
			WarehouseScript.EnterWarehouse(warehouse, targetId, skipAuth: true);
			Utils.DisplayHelpText("You ~b~entered " + targetInfo.ColorNameAndId + "'s warehouse.");
			break;
		case ForceEntryResponse.NoItem:
			Utils.DisplayHelpText("You don't have a ~y~crowbar~s~ to enter this warehouse.");
			break;
		case ForceEntryResponse.Cooldown:
			Utils.DisplayHelpText("~r~You must wait before attempting to enter again.");
			break;
		case ForceEntryResponse.OwnerOffline:
			Utils.DisplayHelpText("~r~The owner of the warehouse is no longer connected. Please, reopen the menu to refresh the player list.");
			break;
		default:
			Utils.DisplayErrorMessage(101, (int)forceEntryResponse);
			break;
		}
	}
}
