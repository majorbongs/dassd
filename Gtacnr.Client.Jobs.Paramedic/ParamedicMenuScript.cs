using System.Collections.Generic;
using Gtacnr.Client.Communication;
using Gtacnr.Localization;
using MenuAPI;

namespace Gtacnr.Client.Jobs.Paramedic;

public class ParamedicMenuScript : Script
{
	private static Dictionary<string, Menu> subMenus = new Dictionary<string, Menu>();

	private static Dictionary<string, MenuItem> menuItems = new Dictionary<string, MenuItem>();

	public static Menu Menu { get; private set; }

	protected override async void OnStarted()
	{
		Menu = new Menu(LocalizationController.S(Entries.Businesses.MENU_PARAMEDIC_TITLE), LocalizationController.S(Entries.Main.MENU_CHOOSE_OPTION));
		Menu.OnItemSelect += OnMainMenuItemSelect;
		menuItems["radio"] = new MenuItem(LocalizationController.S(Entries.Businesses.MENU_RADIO_TITLE), LocalizationController.S(Entries.Businesses.MENU_RADIO_USE_SUBTITLE))
		{
			LeftIcon = MenuItem.Icon.GTACNR_JOB_RADIO
		};
		Menu.AddMenuItem(menuItems["radio"]);
		MenuController.BindMenuItem(Menu, RadioScript.MainMenu, menuItems["radio"]);
		Menu callsMenu = DispatchScript.ParamedicDispatch.CallsMenu;
		Dictionary<string, MenuItem> dictionary = menuItems;
		MenuItem obj = new MenuItem(LocalizationController.S(Entries.Businesses.JOBMENU_CALLS), LocalizationController.S(Entries.Businesses.MENU_PARAMEDIC_CALLS_DESCRIPTION))
		{
			LeftIcon = MenuItem.Icon.GTACNR_JOB_CALLS
		};
		MenuItem menuItem = obj;
		dictionary["calls"] = obj;
		AddSubmenuItem(callsMenu, menuItem);
	}

	private void AddSubmenuItem(Menu subMenu, MenuItem menuItem)
	{
		Menu.AddMenuItem(menuItem);
		MenuController.AddSubmenu(Menu, subMenu);
		MenuController.BindMenuItem(Menu, subMenu, menuItem);
	}

	private void OnMainMenuItemSelect(Menu menu, MenuItem menuItem, int itemIndex)
	{
		if (menuItem == menuItems["radio"])
		{
			RadioScript.MainMenu.ParentMenu = Menu;
		}
	}
}
