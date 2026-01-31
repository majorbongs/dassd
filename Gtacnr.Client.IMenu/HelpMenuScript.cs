using System.Collections.Generic;
using CitizenFX.Core;
using Gtacnr.Client.API;
using Gtacnr.Client.Communication;
using Gtacnr.Localization;
using Gtacnr.Model;
using MenuAPI;

namespace Gtacnr.Client.IMenu;

public class HelpMenuScript : Script
{
	private Dictionary<string, MenuItem> menuItems = new Dictionary<string, MenuItem>();

	public static Menu Menu { get; private set; }

	protected override async void OnStarted()
	{
		while (MainMenuScript.MainMenu == null)
		{
			await BaseScript.Delay(0);
		}
		Menu = new Menu(LocalizationController.S(Entries.Imenu.IMENU_HELP_TITLE), LocalizationController.S(Entries.Imenu.IMENU_HELP_SUBTITLE));
		Menu menu = Menu;
		MenuItem item = (menuItems["block"] = new MenuItem(LocalizationController.S(Entries.Imenu.IMENU_HELP_BLOCK_TITLE), LocalizationController.S(Entries.Imenu.IMENU_HELP_BLOCK_DESCRIPTION)));
		menu.AddMenuItem(item);
		Menu menu2 = Menu;
		item = (menuItems["report"] = new MenuItem(LocalizationController.S(Entries.Imenu.IMENU_HELP_REPORT_TITLE), LocalizationController.S(Entries.Imenu.IMENU_HELP_REPORT_DESCRIPTION)));
		menu2.AddMenuItem(item);
		Menu menu3 = Menu;
		item = (menuItems["blocks"] = new MenuItem(LocalizationController.S(Entries.Main.MENU_BLOCKED_TITLE), LocalizationController.S(Entries.Imenu.IMENU_HELP_BLOCKED_PLAYERS_DESCRIPTION)));
		menu3.AddMenuItem(item);
		Menu menu4 = Menu;
		item = (menuItems["reports"] = new MenuItem(LocalizationController.S(Entries.Imenu.IMENU_HELP_REPORT_LIST_TITLE), LocalizationController.S(Entries.Imenu.IMENU_HELP_REPORT_LIST_DESCRIPTION)));
		menu4.AddMenuItem(item);
		Menu menu5 = Menu;
		Dictionary<string, MenuItem> dictionary = menuItems;
		MenuItem obj = new MenuItem(LocalizationController.S(Entries.Imenu.IMENU_HELP_WIKI_TITLE), LocalizationController.S(Entries.Chatnotifications.NOTI_FANDOM))
		{
			Enabled = false
		};
		item = obj;
		dictionary["guide"] = obj;
		menu5.AddMenuItem(item);
		Menu menu6 = Menu;
		Dictionary<string, MenuItem> dictionary2 = menuItems;
		MenuItem obj2 = new MenuItem(LocalizationController.S(Entries.Imenu.IMENU_HELP_SUPPORT_TITLE), LocalizationController.S(Entries.Imenu.IMENU_HELP_SUPPORT_DESCRIPTION))
		{
			Enabled = false
		};
		item = obj2;
		dictionary2["support"] = obj2;
		menu6.AddMenuItem(item);
		Menu menu7 = Menu;
		Dictionary<string, MenuItem> dictionary3 = menuItems;
		MenuItem obj3 = new MenuItem(LocalizationController.S(Entries.Imenu.IMENU_HELP_RULES_TITLE), LocalizationController.S(Entries.Imenu.IMENU_HELP_RULES_DESCRIPTION))
		{
			Enabled = false
		};
		item = obj3;
		dictionary3["rules"] = obj3;
		menu7.AddMenuItem(item);
		Menu menu8 = Menu;
		Dictionary<string, MenuItem> dictionary4 = menuItems;
		MenuItem obj4 = new MenuItem(LocalizationController.S(Entries.Imenu.IMENU_HELP_PRIVACY_TITLE), LocalizationController.S(Entries.Imenu.IMENU_HELP_PRIVACY_DESCRIPTION))
		{
			Enabled = false
		};
		item = obj4;
		dictionary4["privacy"] = obj4;
		menu8.AddMenuItem(item);
		Menu.OnItemSelect += OnItemSelect;
		MenuController.AddSubmenu(MainMenuScript.MainMenu, Menu);
		MenuController.BindMenuItem(MainMenuScript.MainMenu, Menu, MainMenuScript.MainMenuItems["help"]);
	}

	private async void OnItemSelect(Menu menu, MenuItem menuItem, int itemIndex)
	{
		if (menuItem == menuItems["report"])
		{
			string text = await Utils.GetUserInput(LocalizationController.S(Entries.Imenu.IMENU_HELP_REPORT_INPUT_TITLE), LocalizationController.S(Entries.Main.INPUT_ENTER_ID), "", 5);
			int targetId;
			if (text == null)
			{
				Utils.PlayErrorSound();
				Utils.SendNotification(LocalizationController.S(Entries.Imenu.IMENU_HELP_REPORT_CANCELED));
			}
			else if (int.TryParse(text, out targetId))
			{
				if (LatentPlayers.Get(targetId) != null)
				{
					MenuController.CloseAllMenus();
					await BaseScript.Delay(200);
					ReportMenuScript.OpenReportMenu(targetId);
				}
				else
				{
					Utils.SendNotification(LocalizationController.S(Entries.Imenu.IMENU_HELP_PLAYER_NOT_CONNECTED));
				}
			}
			else
			{
				Utils.SendNotification(LocalizationController.S(Entries.Imenu.IMENU_HELP_INVALID_ID));
			}
		}
		else if (menuItem == menuItems["reports"])
		{
			ReportMenuScript.OpenMyReportsMenu();
		}
		else if (menuItem == menuItems["block"])
		{
			string text2 = await Utils.GetUserInput(LocalizationController.S(Entries.Imenu.IMENU_HELP_BLOCK_INPUT_TITLE), LocalizationController.S(Entries.Main.INPUT_ENTER_ID), "", 5);
			int result;
			if (text2 == null)
			{
				Utils.PlayErrorSound();
				Utils.SendNotification(LocalizationController.S(Entries.Imenu.IMENU_HELP_BLOCKING_CANCELED));
			}
			else if (int.TryParse(text2, out result))
			{
				PlayerState playerState = LatentPlayers.Get(result);
				if (playerState != null)
				{
					MenuController.CloseAllMenus();
					if (BlockScript.Block(playerState.Uid))
					{
						Utils.SendNotification(LocalizationController.S(Entries.Chatnotifications.BLOCK_PLAYER, playerState.Name));
					}
					else
					{
						Utils.SendNotification(LocalizationController.S(Entries.Chatnotifications.UNABLE_BLOCK_PLAYER, playerState.Name));
					}
				}
				else
				{
					Utils.SendNotification(LocalizationController.S(Entries.Imenu.IMENU_HELP_PLAYER_NOT_CONNECTED));
				}
			}
			else
			{
				Utils.SendNotification(LocalizationController.S(Entries.Imenu.IMENU_HELP_INVALID_ID));
			}
		}
		else if (menuItem == menuItems["blocks"])
		{
			MenuController.CloseAllMenus();
			await BaseScript.Delay(200);
			BlockScript.OpenBlockMenu();
		}
	}
}
