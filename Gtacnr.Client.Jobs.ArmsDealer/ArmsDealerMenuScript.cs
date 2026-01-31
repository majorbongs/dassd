using System;
using System.Collections.Generic;
using System.Linq;
using CitizenFX.Core;
using Gtacnr.Client.API;
using Gtacnr.Data;
using Gtacnr.Localization;
using Gtacnr.Model;
using Gtacnr.Model.Enums;
using MenuAPI;

namespace Gtacnr.Client.Jobs.ArmsDealer;

public class ArmsDealerMenuScript : Script
{
	private static Dictionary<string, Menu> subMenus = new Dictionary<string, Menu>();

	private static Dictionary<string, MenuItem> menuItems = new Dictionary<string, MenuItem>();

	private List<SaleInfo> sales = new List<SaleInfo>();

	public static Menu Menu { get; private set; }

	public ArmsDealerMenuScript()
	{
		Gtacnr.Client.API.Jobs.JobChangedEvent += OnJobChangedEvent;
	}

	private void OnJobChangedEvent(object sender, JobArgs e)
	{
		if (e.PreviousJobEnum != JobsEnum.ArmsDealer && e.CurrentJobEnum == JobsEnum.ArmsDealer)
		{
			RefreshJobMenu();
		}
	}

	protected override async void OnStarted()
	{
		while (ServicesMenuScript.Menu == null || StockMenuScript.Menu == null)
		{
			await BaseScript.Delay(0);
		}
		Menu = new Menu(LocalizationController.S(Entries.Businesses.MENU_ARMS_DEALER_TITLE), LocalizationController.S(Entries.Main.MENU_CHOOSE_OPTION));
		Menu.OnMenuOpen += OnMenuOpen;
		RefreshJobMenu();
	}

	private async void RefreshJobMenu()
	{
		while (Menu == null)
		{
			await BaseScript.Delay(0);
		}
		Menu.ClearMenuItems();
		menuItems["items"] = new MenuItem(LocalizationController.S(Entries.Businesses.MENU_STOCK_TITLE), LocalizationController.S(Entries.Imenu.IMENU_INVENTORY_STOCK_DESCR))
		{
			LeftIcon = MenuItem.Icon.GTACNR_STOCK
		};
		Menu.AddMenuItem(menuItems["items"]);
		MenuController.AddSubmenu(Menu, StockMenuScript.Menu);
		MenuController.BindMenuItem(Menu, StockMenuScript.Menu, menuItems["items"]);
		ArmsDealerMenuScript armsDealerMenuScript = this;
		Menu callsMenu = DispatchScript.ArmsDealerDispatch.CallsMenu;
		Dictionary<string, MenuItem> dictionary = menuItems;
		MenuItem obj = new MenuItem(LocalizationController.S(Entries.Businesses.JOBMENU_CALLS), LocalizationController.S(Entries.Businesses.MENU_ARMS_DEALER_CALLS_DESCRIPTION))
		{
			LeftIcon = MenuItem.Icon.GTACNR_JOB_CALLS
		};
		MenuItem menuItem = obj;
		dictionary["calls"] = obj;
		armsDealerMenuScript.AddSubmenuItem(callsMenu, menuItem);
		ArmsDealerMenuScript armsDealerMenuScript2 = this;
		Menu subMenu = (subMenus["sales"] = new Menu(LocalizationController.S(Entries.Businesses.MENU_ARMS_DEALER_TITLE), LocalizationController.S(Entries.Businesses.JOBMENU_RECENT_SALES_SUB)));
		Dictionary<string, MenuItem> dictionary2 = menuItems;
		MenuItem obj2 = new MenuItem(LocalizationController.S(Entries.Businesses.JOBMENU_RECENT_SALES_SUB), LocalizationController.S(Entries.Businesses.JOBMENU_RECENT_SALES_DESCR))
		{
			LeftIcon = MenuItem.Icon.GTACNR_JOB_SALES
		};
		menuItem = obj2;
		dictionary2["sales"] = obj2;
		armsDealerMenuScript2.AddSubmenuItem(subMenu, menuItem);
		subMenus["sales"].OnMenuOpen += OnMenuOpen;
		MenuController.AddSubmenu(Menu, subMenus["sales"]);
		MenuController.BindMenuItem(Menu, subMenus["sales"], menuItems["sales"]);
	}

	private void AddSubmenuItem(Menu subMenu, MenuItem menuItem)
	{
		Menu.AddMenuItem(menuItem);
		MenuController.AddSubmenu(Menu, subMenu);
		MenuController.BindMenuItem(Menu, subMenu, menuItem);
	}

	private void OnMenuOpen(Menu menu)
	{
		if (menu != subMenus["sales"])
		{
			return;
		}
		subMenus["sales"].ClearMenuItems();
		foreach (SaleInfo item2 in sales.OrderByDescending((SaleInfo s) => s.DateTime))
		{
			PlayerState playerState = LatentPlayers.Get(item2.CustomerId) ?? PlayerState.CreateDisconnectedPlayer(item2.CustomerId);
			string text = Gtacnr.Utils.CalculateTimeAgo(item2.DateTime);
			IItemOrService itemDefinition = Gtacnr.Data.Items.GetItemDefinition(item2.ItemId);
			object obj = itemDefinition;
			if (obj == null)
			{
				itemDefinition = Gtacnr.Data.Items.GetServiceDefinition(item2.ItemId);
				obj = itemDefinition;
				if (obj == null)
				{
					itemDefinition = Gtacnr.Data.Items.GetWeaponDefinition(item2.ItemId);
					obj = itemDefinition;
					if (obj == null)
					{
						itemDefinition = Gtacnr.Data.Items.GetAmmoDefinition(item2.ItemId);
						obj = itemDefinition ?? Gtacnr.Data.Items.GetWeaponComponentDefinition(item2.ItemId);
					}
				}
			}
			IItemOrService itemOrService = (IItemOrService)obj;
			MenuItem menuItem = new MenuItem("~g~" + item2.Price.ToCurrencyString());
			menuItem.Label = "~c~" + text;
			menuItem.Description = $"Buyer: {playerState.ColorTextCode}{playerState.Name} ({playerState.Id})~n~Item: {itemOrService.Name}~n~~s~Amount: {playerState.ColorTextCode}{item2.Amount:0.##}";
			MenuItem item = menuItem;
			subMenus["sales"].AddMenuItem(item);
		}
		if (sales.Count() == 0)
		{
			subMenus["sales"].AddMenuItem(new MenuItem("No sales :(", "There are currently no sales to display.")
			{
				Enabled = false
			});
		}
	}

	[EventHandler("gtacnr:jobs:onSale")]
	private void OnSale(int playerId, string itemId, float amount, int price)
	{
		sales.Add(new SaleInfo
		{
			DateTime = DateTime.UtcNow,
			CustomerId = playerId,
			ItemId = itemId,
			Amount = amount,
			Price = price
		});
	}

	[EventHandler("gtacnr:jobs:onServiceSale")]
	private void OnServiceSale(int playerId, string serviceId, int price, int shopFee, string jExtraData)
	{
		sales.Add(new SaleInfo
		{
			DateTime = DateTime.UtcNow,
			CustomerId = playerId,
			ItemId = serviceId,
			Amount = 1f,
			Price = price
		});
	}
}
