using System.Collections.Generic;
using CitizenFX.Core;
using Gtacnr.Client.API;
using Gtacnr.Client.Communication;
using Gtacnr.Localization;
using Gtacnr.Model.Enums;
using MenuAPI;

namespace Gtacnr.Client.Jobs.Police;

public class PoliceMenuScript : Script
{
	private static Dictionary<string, Menu> subMenus = new Dictionary<string, Menu>();

	private static Dictionary<string, MenuItem> menuItems = new Dictionary<string, MenuItem>();

	private static DispatchFilter callFilter;

	public static Menu Menu { get; private set; }

	public static DispatchFilter CallFilter => callFilter;

	protected override async void OnStarted()
	{
		Menu = new Menu(LocalizationController.S(Entries.Businesses.MENU_POLICEOFFICER_TITLE), LocalizationController.S(Entries.Main.MENU_CHOOSE_OPTION));
		Menu.OnItemSelect += OnMenuItemSelect;
		Menu.OnListIndexChange += OnMenuListIndexChange;
		Menu.OnListItemSelect += OnMenuListItemSelect;
		Menu.OnMenuOpen += OnMenuOpen;
		Menu.ClearMenuItems();
		menuItems["radio"] = new MenuItem(LocalizationController.S(Entries.Businesses.MENU_RADIO_TITLE), LocalizationController.S(Entries.Businesses.MENU_RADIO_USE_SUBTITLE))
		{
			LeftIcon = MenuItem.Icon.GTACNR_JOB_RADIO
		};
		Menu.AddMenuItem(menuItems["radio"]);
		MenuController.BindMenuItem(Menu, RadioScript.MainMenu, menuItems["radio"]);
		Menu callsMenu = DispatchScript.PoliceDispatch.CallsMenu;
		Dictionary<string, MenuItem> dictionary = menuItems;
		MenuItem obj = new MenuItem(LocalizationController.S(Entries.Businesses.JOBMENU_CALLS), LocalizationController.S(Entries.Businesses.MENU_POLICEOFFICER_CALLS_DESCRIPTION))
		{
			LeftIcon = MenuItem.Icon.GTACNR_JOB_CALLS
		};
		MenuItem menuItem = obj;
		dictionary["calls"] = obj;
		AddSubmenuItem(callsMenu, menuItem);
		callFilter = Preferences.PoliceCallFilter.Get();
		menuItems["callFilter"] = new MenuListItem("Dispatch Filter", new string[4] { "All calls", "Violent crimes only", "Major felonies only", "No calls" }, (int)callFilter);
		Menu.AddMenuItem(menuItems["callFilter"]);
		menuItems["voice"] = new MenuListItem("Voice", new string[0], 0)
		{
			Description = "Press the select key to preview"
		};
		Menu.AddMenuItem(menuItems["voice"]);
	}

	private void OnMenuOpen(Menu menu)
	{
		if (menu != Menu)
		{
			return;
		}
		Sex freemodePedSex = Utils.GetFreemodePedSex(Game.PlayerPed);
		int listIndex = Preferences.PoliceVoiceIdx.Get();
		MenuListItem menuListItem = menuItems["voice"] as MenuListItem;
		menuListItem.ListIndex = listIndex;
		menuListItem.Text = $"Voice ({freemodePedSex})";
		menuListItem.ListItems = new List<string>();
		int num = 0;
		foreach (string item in PoliceVoices.Voices[freemodePedSex])
		{
			_ = item;
			menuListItem.ListItems.Add($"#{++num}");
		}
	}

	private void OnMenuListIndexChange(Menu menu, MenuListItem listItem, int oldSelectionIndex, int newSelectionIndex, int itemIndex)
	{
		if (listItem == menuItems["callFilter"])
		{
			callFilter = (DispatchFilter)newSelectionIndex;
			Preferences.PoliceCallFilter.Set(callFilter);
		}
		else if (listItem == menuItems["voice"])
		{
			Preferences.PoliceVoiceIdx.Set(newSelectionIndex);
		}
	}

	private void OnMenuListItemSelect(Menu menu, MenuListItem listItem, int selectedIndex, int itemIndex)
	{
		if (listItem == menuItems["voice"])
		{
			Sex freemodePedSex = Utils.GetFreemodePedSex(Game.PlayerPed);
			int index = Preferences.PoliceVoiceIdx.Get();
			string voice = PoliceVoices.GetVoice(freemodePedSex, index);
			string text = new string[4] { "ARREST_PLAYER", "DRAW_GUN", "FOOT_CHASE", "STOP_ON_FOOT_MEGAPHONE" }.Random();
			BaseScript.TriggerEvent("gtacnr:playAmbientSpeechFromPed", new object[4]
			{
				text,
				voice,
				((Entity)Game.PlayerPed).NetworkId,
				"SPEECH_PARAMS_FORCE"
			});
		}
	}

	private void AddSubmenuItem(Menu subMenu, MenuItem menuItem)
	{
		Menu.AddMenuItem(menuItem);
		MenuController.AddSubmenu(Menu, subMenu);
		MenuController.BindMenuItem(Menu, subMenu, menuItem);
	}

	private void OnMenuItemSelect(Menu menu, MenuItem menuItem, int itemIndex)
	{
		if (menuItem == menuItems["radio"])
		{
			RadioScript.MainMenu.ParentMenu = Menu;
		}
	}
}
