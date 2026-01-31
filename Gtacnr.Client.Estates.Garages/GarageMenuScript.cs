using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Gtacnr.Client.API;
using Gtacnr.Client.Jobs.Police.Arrest;
using Gtacnr.Client.Premium;
using Gtacnr.Localization;
using Gtacnr.Model;
using Gtacnr.Model.Enums;
using Gtacnr.ResponseCodes;
using MenuAPI;

namespace Gtacnr.Client.Estates.Garages;

public class GarageMenuScript : Script
{
	private static GarageMenuScript instance;

	private Menu mainMenu;

	private Menu playersMenu;

	private EstatePlayerAction? selectedPlayerAction;

	private Dictionary<string, MenuItem> menuItems = new Dictionary<string, MenuItem>();

	private bool isBusy;

	private const string NOT_CONNECTED_STR = "~r~The owner of the garage is no longer connected. Please, reopen the menu to refresh the player list.";

	public GarageMenuScript()
	{
		instance = this;
		mainMenu = new Menu("Garage", "Garage")
		{
			MaxDistance = 7.5f
		};
		MenuController.AddMenu(mainMenu);
		playersMenu = new Menu("Garage", "Owners")
		{
			MaxDistance = 7.5f
		};
		MenuController.AddMenu(playersMenu);
		mainMenu.OnItemSelect += OnItemSelected;
		mainMenu.OnMenuOpen += OnMenuOpen;
		playersMenu.OnMenuOpen += OnMenuOpen;
		playersMenu.OnItemSelect += OnPlayerSelected;
	}

	public static void OpenMenu()
	{
		if (!instance.mainMenu.Visible)
		{
			instance.mainMenu.OpenMenu();
		}
	}

	public static void CloseMenu()
	{
		instance.mainMenu.CloseMenu();
	}

	private void OnMenuOpen(Menu menu)
	{
		if (menu == playersMenu)
		{
			RefreshPlayersMenu();
		}
		else if (menu == mainMenu)
		{
			RefreshMenu();
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
			Garage garage = GarageScript.ClosestGarage;
			if (garage == null)
			{
				isBusy = false;
				MenuController.CloseAllMenus();
			}
			else if (Selected("buy"))
			{
				if (await Money.GetCachedBalanceOrFetch(AccountType.Bank) < garage.Value)
				{
					Utils.DisplayHelpText(LocalizationController.S(Entries.Main.NOT_ENOUGH_MONEY_BANK_ACCOUNT));
					isBusy = false;
				}
				else if (await Utils.ShowConfirm("Do you really want to purchase ~y~" + garage.Name + " ~s~ for ~g~" + garage.Value.ToCurrencyString() + "~s~?"))
				{
					BuyPropertyResponse resp = (BuyPropertyResponse)(await TriggerServerEventAsync<int>("gtacnr:garages:buy", new object[1] { garage.Id }));
					await BaseScript.Delay(500);
					switch (resp)
					{
					case BuyPropertyResponse.Success:
						Utils.DisplayHelpText("You purchased ~y~" + garage.Name + " ~s~for ~g~" + garage.Value.ToCurrencyString() + "~s~.");
						GarageScript.OnBuyGarage(garage);
						GarageScript.RefreshBlips();
						RefreshMenu();
						break;
					case BuyPropertyResponse.Level:
						Utils.DisplayHelpText("~r~You don't have the required level to purchase this garage.", playSound: false);
						Utils.PlayErrorSound();
						break;
					case BuyPropertyResponse.MembershipTier:
						Utils.DisplayHelpText("~r~You don't have the required membership tier to purchase this garage.", playSound: false);
						Utils.PlayErrorSound();
						break;
					case BuyPropertyResponse.WantedLevel:
						Utils.DisplayHelpText("~r~You are wanted by the police. Come back when you're clean!", playSound: false);
						Utils.PlayErrorSound();
						break;
					default:
						Utils.DisplayErrorMessage(199, (int)resp);
						break;
					}
				}
			}
			else if (Selected("enter"))
			{
				GarageScript.EnterGarage(garage, API.GetPlayerServerId(API.PlayerId()));
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

	private async void RefreshMenu()
	{
		_ = 1;
		try
		{
			Garage garage = GarageScript.ClosestGarage;
			if (garage == null)
			{
				mainMenu.Visible = false;
				return;
			}
			mainMenu.ClearMenuItems();
			mainMenu.MenuSubtitle = garage.Name;
			MembershipTier currentTier = MembershipScript.GetCurrentMembershipTier();
			int levelByXP = Gtacnr.Utils.GetLevelByXP(await Users.GetXP());
			string membershipStr = "";
			bool locked = false;
			if ((int)garage.MembershipTier > 0)
			{
				if ((int)currentTier < (int)garage.MembershipTier)
				{
					membershipStr = "~n~~p~" + LocalizationController.S(Entries.Businesses.ITEM_ATTRIBUTE_REQUIRES_MEMBERSHIP, Gtacnr.Utils.GetDescription(garage.MembershipTier), ExternalLinks.Collection.Store);
					locked = true;
				}
				else
				{
					membershipStr = "~n~~p~" + LocalizationController.S(Entries.Businesses.ITEM_ATTRIBUTE_UNLOCKED_WITH_MEMBERSHIP, Gtacnr.Utils.GetDescription(currentTier)) + "~s~";
				}
			}
			string levelStr = "";
			if (garage.RequiredLevel > 0)
			{
				membershipStr = "~n~~b~" + LocalizationController.S(Entries.Businesses.ITEM_ATTRIBUTE_UNLOCK_LEVEL, garage.RequiredLevel) + "~s~";
				if (levelByXP < garage.RequiredLevel)
				{
					locked = true;
				}
			}
			MenuItem item;
			if (!GarageScript.DoesPlayerOwnGarage(garage))
			{
				string label = ((await Money.GetCachedBalanceOrFetch(AccountType.Bank) >= garage.Value) ? "~g~" : "~r~") + garage.Value.ToCurrencyString();
				if (garage.Coins > 0)
				{
					label = $"~y~{garage.Coins} Coins";
				}
				Menu menu = mainMenu;
				item = (menuItems["buy"] = new MenuItem("Purchase")
				{
					Description = "Garages allow you to store stolen and owned vehicles.~s~~n~Size: " + Gtacnr.Utils.GetDescription(garage.Interior.Size) + "~s~" + levelStr + membershipStr,
					Label = label,
					Enabled = !locked,
					RightIcon = (locked ? MenuItem.Icon.LOCK : MenuItem.Icon.NONE)
				});
				menu.AddMenuItem(item);
			}
			else
			{
				Menu menu2 = mainMenu;
				item = (menuItems["enter"] = new MenuItem("Enter", "Enter your garage."));
				menu2.AddMenuItem(item);
				Menu menu3 = mainMenu;
				Dictionary<string, MenuItem> dictionary = menuItems;
				MenuItem obj = new MenuItem("Sell", "Sell your ~b~garage ~s~to the server.\n~r~This feature is not available yet.")
				{
					Label = "~g~" + ((double)garage.Value * 0.6).ToLong().ToCurrencyString(),
					Enabled = false
				};
				item = obj;
				dictionary["sell"] = obj;
				menu3.AddMenuItem(item);
			}
			Menu menu4 = mainMenu;
			item = (menuItems["knock"] = new MenuItem("~y~Knock")
			{
				Description = "Ask an owner of this garage to let you in.",
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
					Description = "Enter the garage forcibly if you saw a ~o~wanted suspect ~s~enter.",
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
					Description = "Break into this garage.",
					Label = "›"
				};
				item = obj3;
				dictionary3["breakIn"] = obj3;
				menu6.AddMenuItem(item);
				MenuController.BindMenuItem(mainMenu, playersMenu, menuItems["breakIn"]);
			}
			if (GarageScript.DoesPlayerOwnGarage(garage))
			{
				mainMenu.AddMenuItem(new MenuItem("Information")
				{
					Description = "Size: " + Gtacnr.Utils.GetDescription(garage.Interior.Size) + "~s~" + levelStr + membershipStr + "~n~~g~Owned"
				});
			}
			Menu menu7 = mainMenu;
			item = (menuItems["players"] = new MenuItem("Owners", "A list of connected players that own this garage.")
			{
				Label = "›"
			});
			menu7.AddMenuItem(item);
			MenuController.BindMenuItem(mainMenu, playersMenu, menuItems["players"]);
		}
		catch (Exception exception)
		{
			Print(exception);
		}
	}

	private async void RefreshPlayersMenu()
	{
		playersMenu.ClearMenuItems();
		playersMenu.AddLoadingMenuItem();
		List<int> list = (await TriggerServerEventAsync<string>("gtacnr:garages:getOnlineOwners", new object[1] { GarageScript.ClosestGarage.Id })).Unjson<List<int>>();
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
			playersMenu.AddMenuItem(new MenuItem("No players :(", "There are no online players that own this garage."));
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
			Utils.DisplayHelpText("~r~The owner of the garage is no longer connected. Please, reopen the menu to refresh the player list.");
			return;
		}
		isBusy = true;
		try
		{
			Garage closestGarage = GarageScript.ClosestGarage;
			if (closestGarage == null)
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
					await Knock(closestGarage, num);
					break;
				case EstatePlayerAction.ForceEntry:
					await ForceEntry(closestGarage, num);
					break;
				case EstatePlayerAction.BreakIn:
					await BreakInEntry(closestGarage, num);
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

	private async Task Knock(Garage garage, int targetId)
	{
		PlayerState targetInfo = LatentPlayers.Get(targetId);
		KnockAtPropertyResponse knockAtPropertyResponse = (KnockAtPropertyResponse)(await TriggerServerEventAsync<int>("gtacnr:garages:knock", new object[2] { garage.Id, targetId }));
		switch (knockAtPropertyResponse)
		{
		case KnockAtPropertyResponse.Success:
			Utils.DisplayHelpText("You knocked at " + targetInfo.ColorNameAndId + "'s garage. Wait for them to answer.");
			break;
		case KnockAtPropertyResponse.Cooldown:
			Utils.DisplayHelpText("~r~You must wait before knocking again.");
			break;
		case KnockAtPropertyResponse.OwnerOffline:
			Utils.DisplayHelpText("~r~The owner of the garage is no longer connected. Please, reopen the menu to refresh the player list.");
			break;
		default:
			Utils.DisplayErrorMessage(100, (int)knockAtPropertyResponse);
			break;
		}
	}

	private async Task ForceEntry(Garage garage, int targetId)
	{
		PlayerState targetInfo = LatentPlayers.Get(targetId);
		BaseScript.TriggerEvent("dpemotes:cancelEmoteImmediately", new object[0]);
		Utils.RemoveAllAttachedProps();
		ForceEntryResponse forceEntryResponse = (ForceEntryResponse)(await TriggerServerEventAsync<int>("gtacnr:garages:forceEntry", new object[2] { garage.Id, targetId }));
		switch (forceEntryResponse)
		{
		case ForceEntryResponse.Success:
			GarageScript.EnterGarage(garage, targetId, skipAuth: true);
			Utils.DisplayHelpText("You ~b~entered " + targetInfo.ColorNameAndId + "'s garage.");
			break;
		case ForceEntryResponse.NoWarrant:
			Utils.DisplayHelpText("You don't have a ~o~warrant ~s~to enter this garage. You can only enter when you spot a ~o~wanted suspect ~s~enter.");
			break;
		case ForceEntryResponse.Cooldown:
			Utils.DisplayHelpText("~r~You must wait before attempting to enter again.");
			break;
		case ForceEntryResponse.OwnerOffline:
			Utils.DisplayHelpText("~r~The owner of the garage is no longer connected. Please, reopen the menu to refresh the player list.");
			break;
		default:
			Utils.DisplayErrorMessage(101, (int)forceEntryResponse);
			break;
		}
	}

	private async Task BreakInEntry(Garage garage, int targetId)
	{
		if (CuffedScript.IsCuffed || SurrenderScript.IsSurrendered || Game.PlayerPed.IsBeingStunned || CuffedScript.IsBeingCuffedOrUncuffed)
		{
			Utils.PlayErrorSound();
			return;
		}
		PlayerState targetInfo = LatentPlayers.Get(targetId);
		BaseScript.TriggerEvent("dpemotes:cancelEmoteImmediately", new object[0]);
		Utils.RemoveAllAttachedProps();
		ForceEntryResponse forceEntryResponse = (ForceEntryResponse)(await TriggerServerEventAsync<int>("gtacnr:garages:breakIn", new object[2] { garage.Id, targetId }));
		switch (forceEntryResponse)
		{
		case ForceEntryResponse.Success:
			GarageScript.EnterGarage(garage, targetId, skipAuth: true);
			Utils.DisplayHelpText("You ~b~entered " + targetInfo.ColorNameAndId + "'s garage.");
			break;
		case ForceEntryResponse.NoItem:
			Utils.DisplayHelpText("You don't have a ~y~crowbar~s~ to enter this garage.");
			break;
		case ForceEntryResponse.Cooldown:
			Utils.DisplayHelpText("~r~You must wait before attempting to enter again.");
			break;
		case ForceEntryResponse.OwnerOffline:
			Utils.DisplayHelpText("~r~The owner of the garage is no longer connected. Please, reopen the menu to refresh the player list.");
			break;
		default:
			Utils.DisplayErrorMessage(101, (int)forceEntryResponse);
			break;
		}
	}
}
