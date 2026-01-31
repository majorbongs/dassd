using System;
using System.Collections.Generic;
using System.Linq;
using Gtacnr.Client.API;
using Gtacnr.Client.IMenu;
using Gtacnr.Data;
using Gtacnr.Model;
using Gtacnr.Model.Enums;
using MenuAPI;

namespace Gtacnr.Client.Businesses.Airports;

public class AirportScript : Script
{
	private Menu boardingMenu;

	private MenuItem boardingMenuItem;

	private bool boardMenuItemAdded;

	public static IReadOnlyDictionary<string, Airline> Airlines { get; } = Gtacnr.Utils.LoadJson<List<Airline>>("data/vehicles/airlines.json").ToDictionary((Airline kvp) => kvp.Id, (Airline kvp) => kvp);

	protected override void OnStarted()
	{
		ShoppingScript.MenuOpening += OnShoppingMenuOpening;
		ShoppingScript.ItemPurchased += OnItemPurchased;
		boardingMenuItem = new MenuItem("~b~Board");
		boardingMenu = new Menu("Boarding", "Select a flight");
		boardingMenu.OnItemSelect += OnMenuItemSelect;
	}

	private void OnShoppingMenuOpening(object sender, EventArgs e)
	{
		Business closestBusiness = BusinessScript.ClosestBusiness;
		if (closestBusiness == null || closestBusiness.Airport == null)
		{
			ShoppingScript.RemoveExternalMenuItem(BusinessType.Airport, boardingMenuItem);
			boardMenuItemAdded = false;
		}
		else if (closestBusiness.Airport.CanBoard)
		{
			if (!boardMenuItemAdded)
			{
				ShoppingScript.AddExternalMenuItem(BusinessType.Airport, boardingMenuItem);
				ShoppingScript.BindExternalMenuItem(boardingMenu, boardingMenuItem);
				boardMenuItemAdded = true;
			}
			RefreshBoardingMenuAsync();
		}
	}

	private void OnItemPurchased(object sender, EventArgs e)
	{
		if (BusinessScript.ClosestBusiness?.Airport?.CanBoard == true)
		{
			RefreshBoardingMenuAsync();
		}
	}

	private async void RefreshBoardingMenuAsync()
	{
		boardingMenu.ClearMenuItems();
		if (Gtacnr.Client.API.Crime.CachedWantedLevel > 1)
		{
			boardingMenu.AddMenuItem(new MenuItem("Error :(", "You can't board a plane when you are ~o~wanted~s~.")
			{
				Enabled = false
			});
			return;
		}
		IEnumerable<InventoryEntry> source = InventoryMenuScript.Cache;
		if (InventoryMenuScript.ShouldRefreshCache)
		{
			source = await InventoryMenuScript.ReloadInventory();
		}
		foreach (InventoryEntry item2 in source.Where((InventoryEntry e) => e.Amount > 0f))
		{
			InventoryItem itemDefinition = Gtacnr.Data.Items.GetItemDefinition(item2.ItemId ?? "");
			if (itemDefinition != null && itemDefinition.GetExtraDataBool("IsPlaneTicket") && !(itemDefinition.GetExtraDataString("Origin") != BusinessScript.ClosestBusiness.Id))
			{
				string extraDataString = itemDefinition.GetExtraDataString("Destination");
				string extraDataString2 = itemDefinition.GetExtraDataString("Airline");
				Business business = BusinessScript.Businesses[extraDataString];
				Airline airline = Airlines[extraDataString2];
				MenuItem item = new MenuItem(business.Airport.Name, "Board the " + airline.Name + " flight to " + business.Airport.Name + " now.")
				{
					Label = airline.Name,
					ItemData = itemDefinition
				};
				boardingMenu.AddMenuItem(item);
			}
		}
		if (boardingMenu.GetMenuItems().Count == 0)
		{
			boardingMenu.AddMenuItem(new MenuItem("No valid ticket :(", "You must buy a valid ~y~ticket ~s~before boarding.")
			{
				Enabled = false
			});
		}
	}

	private async void OnMenuItemSelect(Menu menu, MenuItem menuItem, int itemIndex)
	{
		object itemData = menuItem.ItemData;
		if (itemData is InventoryItem itemInfo && itemInfo.GetExtraDataBool("IsPlaneTicket"))
		{
			MenuController.CloseAllMenus();
			ResponseCode responseCode = await TriggerServerEventAsync("gtacnr:airport:board", itemInfo.Id);
			if (responseCode == ResponseCode.Success)
			{
				BoardPlane(itemInfo);
			}
			else
			{
				Utils.DisplayError(responseCode, "", "OnMenuItemSelect");
			}
		}
	}

	private async void BoardPlane(InventoryItem ticketItem)
	{
		string extraDataString = ticketItem.GetExtraDataString("Origin");
		_ = BusinessScript.Businesses[extraDataString];
		string extraDataString2 = ticketItem.GetExtraDataString("Destination");
		Business destination = BusinessScript.Businesses[extraDataString2];
		ResponseCode responseCode = await TriggerServerEventAsync("gtacnr:airport:fly");
		if (responseCode == ResponseCode.Success)
		{
			await Utils.TeleportToCoords(destination.Airport.ArrivalCoords.XYZ(), destination.Airport.ArrivalCoords.W, Utils.TeleportFlags.VisualEffects, 5000);
			Utils.DisplayHelpText("Welcome to ~b~" + destination.Airport.Name + "~s~!");
		}
		else
		{
			Utils.DisplayError(responseCode, "", "BoardPlane");
		}
	}
}
