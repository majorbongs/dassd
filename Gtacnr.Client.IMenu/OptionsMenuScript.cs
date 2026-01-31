using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Gtacnr.Client.Admin;
using Gtacnr.Client.API;
using Gtacnr.Client.Characters.Lifecycle;
using Gtacnr.Client.HUD;
using Gtacnr.Client.Items;
using Gtacnr.Client.Keybinder;
using Gtacnr.Client.Premium;
using Gtacnr.Client.Vehicles.Fuel;
using Gtacnr.Client.Weapons;
using Gtacnr.Data;
using Gtacnr.Localization;
using Gtacnr.Model;
using Gtacnr.Model.Enums;
using Gtacnr.ResponseCodes;
using MenuAPI;

namespace Gtacnr.Client.IMenu;

public class OptionsMenuScript : Script
{
	private class LocaleSettings
	{
		public Dictionary<string, string> EnabledLocales { get; set; } = new Dictionary<string, string>();

		public string FallbackLocale { get; set; }

		public bool DisplayIncompleteWarning { get; set; }
	}

	private class AccountServiceInfo
	{
		public string ColorString { get; set; }

		public string Name { get; set; }

		public MenuItem.Icon Icon { get; set; }

		public override string ToString()
		{
			return ColorString + Name;
		}
	}

	private Dictionary<string, Menu> subMenus = new Dictionary<string, Menu>();

	private Dictionary<string, MenuItem> menuItems = new Dictionary<string, MenuItem>();

	private Dictionary<string, string> linkedServices = new Dictionary<string, string>();

	private Dictionary<string, AccountServiceInfo> enabledServices = Gtacnr.Utils.LoadJson<Dictionary<string, AccountServiceInfo>>("data/externalAccounts.json");

	private string currentHotkey;

	private bool isRefreshingLinkedServices;

	private bool isRefreshingSubscriptions;

	private Dictionary<string, string> supportedLocales = new Dictionary<string, string> { { "", "Default (Auto-Detect)" } };

	public static OptionsMenuScript Instance { get; private set; }

	public static Menu Menu { get; private set; }

	public OptionsMenuScript()
	{
		Instance = this;
	}

	protected override async void OnStarted()
	{
		while (MainMenuScript.MainMenu == null)
		{
			await BaseScript.Delay(0);
		}
		Menu = new Menu(LocalizationController.S(Entries.Player.MENU_OPTIONS_TITLE), LocalizationController.S(Entries.Player.MENU_OPTIONS_SUBTITLE));
		MenuController.AddSubmenu(MainMenuScript.MainMenu, Menu);
		MenuController.BindMenuItem(MainMenuScript.MainMenu, Menu, MainMenuScript.MainMenuItems["options"]);
		string text = await Authentication.GetAccountName();
		AddSubmenuItem("account", Menu, new Menu(LocalizationController.S(Entries.Player.MENU_OPTIONS_ACCOUNT_TITLE), text), new MenuItem(LocalizationController.S(Entries.Player.MENU_OPTIONS_ACCOUNT_TITLE), LocalizationController.S(Entries.Player.MENU_OPTIONS_ACCOUNT_SUBTITLE, text))
		{
			LeftIcon = MenuItem.Icon.GTACNR_ACCOUNT
		}, addOpenHandler: false, addSelectHandler: true);
		AddSubmenuItem("subscriptions", subMenus["account"], new Menu(LocalizationController.S(Entries.Player.MENU_OPTIONS_ACCOUNT_TITLE), LocalizationController.S(Entries.Player.MENU_OPTIONS_ACCOUNT_SUBHISTORY_SUBTITLE)), new MenuItem(LocalizationController.S(Entries.Player.MENU_OPTIONS_ACCOUNT_SUBSCRIPTION))
		{
			LeftIcon = MenuItem.Icon.GTACNR_MEMBERSHIP
		});
		MembershipScript.SubscriptionInfoUpdated = (EventHandler)Delegate.Combine(MembershipScript.SubscriptionInfoUpdated, (EventHandler)delegate
		{
			RefreshSubscriptions();
		});
		AddSubmenuItem(LocalizationController.S(Entries.Player.MENU_OPTIONS_ACCOUNT_PURCHASES_SUBTITLE), subMenus["account"], PurchaseableItemsMenuScript.MainMenu, PurchaseableItemsMenuScript.MainMenuItem);
		PurchaseableItemsMenuScript.MainMenu.OnMenuOpen += OnPurchaseableItemsMenuOpen;
		Menu menu = subMenus["account"];
		Dictionary<string, MenuItem> dictionary = menuItems;
		MenuItem obj = new MenuItem(LocalizationController.S(Entries.Main.BTN_REFRESH), LocalizationController.S(Entries.Player.MENU_OPTIONS_ACCOUNT_REFRESH_DESC))
		{
			LeftIcon = MenuItem.Icon.GTACNR_REFRESH
		};
		MenuItem item = obj;
		dictionary["refreshMember"] = obj;
		menu.AddMenuItem(item);
		PurchaseableItem definition = PurchaseableItems.GetDefinition("name_change_token_nt");
		Menu menu2 = subMenus["account"];
		item = (menuItems["changeName"] = new MenuItem(LocalizationController.S(Entries.Player.MENU_OPTIONS_ACCOUNT_CHANGE_NAME), LocalizationController.S(Entries.Player.MENU_OPTIONS_ACCOUNT_CHANGE_NAME_DESC, definition?.Name ?? "<error>"))
		{
			LeftIcon = MenuItem.Icon.GTACNR_REGISTRATION
		});
		menu2.AddMenuItem(item);
		AddSubmenuItem("services", subMenus["account"], new Menu(LocalizationController.S(Entries.Player.MENU_OPTIONS_ACCOUNT_LINKED_SERVICES), LocalizationController.S(Entries.Player.MENU_OPTIONS_ACCOUNT_LINKED_SERVICES)), new MenuItem(LocalizationController.S(Entries.Player.MENU_OPTIONS_ACCOUNT_LINKED_SERVICES), LocalizationController.S(Entries.Player.MENU_OPTIONS_ACCOUNT_LINKED_SERVICES_DESC))
		{
			LeftIcon = MenuItem.Icon.GTACNR_LINK
		}, addOpenHandler: false, addSelectHandler: true);
		subMenus["services"].InstructionalButtons.Add((Control)327, LocalizationController.S(Entries.Main.BTN_REFRESH));
		subMenus["services"].ButtonPressHandlers.Add(new Menu.ButtonPressHandler((Control)327, MenuAPI.Menu.ControlPressCheckType.JUST_PRESSED, async delegate
		{
			await RefreshLinkedServices();
			Utils.PlaySelectSound();
		}, disableControl: true));
		RefreshLinkedServices();
		string text2;
		string label;
		if (linkedServices.ContainsKey("email"))
		{
			text2 = LocalizationController.S(Entries.Player.MENU_OPTIONS_ACCOUNT_CHANGE_EMAIL);
			label = linkedServices["email"];
		}
		else
		{
			text2 = LocalizationController.S(Entries.Player.MENU_OPTIONS_ACCOUNT_ADD_EMAIL);
			label = "";
		}
		Menu menu3 = subMenus["account"];
		item = (menuItems["changeEmail"] = new MenuItem(text2, LocalizationController.S(Entries.Player.MENU_OPTIONS_ACCOUNT_LINK_EMAIL_DESC))
		{
			Label = label,
			LeftIcon = MenuItem.Icon.GTACNR_LINK_EMAIL
		});
		menu3.AddMenuItem(item);
		Menu menu4 = subMenus["account"];
		item = (menuItems["changePassword"] = new MenuItem(LocalizationController.S(Entries.Player.MENU_OPTIONS_ACCOUNT_CHANGE_PASSWORD), LocalizationController.S(Entries.Player.MENU_OPTIONS_ACCOUNT_CHANGE_PASSWORD_DESC))
		{
			LeftIcon = MenuItem.Icon.GTACNR_CHANGE_PASS
		});
		menu4.AddMenuItem(item);
		AddSubmenuItem("display", Menu, new Menu(LocalizationController.S(Entries.Player.MENU_OPTIONS_DISPLAY_TITLE), LocalizationController.S(Entries.Player.MENU_OPTIONS_DISPLAY_SUBTITLE)), new MenuItem(LocalizationController.S(Entries.Player.MENU_OPTIONS_DISPLAY_TITLE), LocalizationController.S(Entries.Player.MENU_OPTIONS_DISPLAY_SUBTITLE))
		{
			LeftIcon = MenuItem.Icon.GTACNR_DISPLAY
		}, addOpenHandler: false, addSelectHandler: true, addListIndexChangedHandler: true);
		string currentLanguage = LocalizationController.CurrentLanguage;
		bool flag = Preferences.MenusLeftAligned.Get();
		int index = (int)Preferences.DeathFeedMode.Get();
		bool flag2 = Preferences.SpeedometerEnabled.Get();
		bool flag3 = Preferences.AltimeterEnabled.Get();
		int index2 = Preferences.PropertyBlipsMode.Get();
		bool flag4 = Preferences.HealthPercentEnabled.Get();
		bool flag5 = Preferences.OverheadSignsEnabled.Get();
		bool flag6 = Preferences.LowFuelPrompt.Get();
		bool flag7 = Preferences.AlwaysShowFuelBar.Get();
		bool flag8 = Preferences.ReticleEnabled.Get();
		bool flag9 = Preferences.ShowPlayerNameTagsOnBlips.Get();
		bool flag10 = Preferences.XPBarHidden.Get();
		string text3 = "\n" + LocalizationController.S(Entries.Player.MENU_OPTIONS_DISPLAY_LANGUAGE_WARNING);
		LocaleSettings localeSettings = Gtacnr.Utils.LoadJson<LocaleSettings>("data/localization.json");
		if (localeSettings != null)
		{
			foreach (KeyValuePair<string, string> enabledLocale in localeSettings.EnabledLocales)
			{
				supportedLocales.Add(enabledLocale.Key, enabledLocale.Value);
			}
			if (!localeSettings.DisplayIncompleteWarning)
			{
				text3 = "";
			}
		}
		Menu menu5 = subMenus["display"];
		item = (menuItems["locale"] = new MenuListItem(LocalizationController.S(Entries.Player.MENU_OPTIONS_DISPLAY_LANGUAGE_TEXT), supportedLocales.Select<KeyValuePair<string, string>, string>((KeyValuePair<string, string> kvp) => kvp.Value).ToList(), supportedLocales.Keys.ToList().IndexOf(currentLanguage), LocalizationController.S(Entries.Player.MENU_OPTIONS_DISPLAY_LANGUAGE_DESC) + text3));
		menu5.AddMenuItem(item);
		Menu menu6 = subMenus["display"];
		item = (menuItems["toggleMenuPos"] = new MenuListItem(LocalizationController.S(Entries.Player.MENU_OPTIONS_DISPLAY_CHAT_POS_TEXT), new List<string>
		{
			LocalizationController.S(Entries.Player.MENU_OPTIONS_DISPLAY_CHAT_POS_OPTION_1),
			LocalizationController.S(Entries.Player.MENU_OPTIONS_DISPLAY_CHAT_POS_OPTION_2)
		}, flag ? 1 : 0)
		{
			Description = LocalizationController.S(Entries.Player.MENU_OPTIONS_DISPLAY_CHAT_POS_DESC)
		});
		menu6.AddMenuItem(item);
		Menu menu7 = subMenus["display"];
		item = (menuItems["deathFeed"] = new MenuListItem(LocalizationController.S(Entries.Player.MENU_OPTIONS_DISPLAY_DEATH_FEED_TEXT), new List<string>
		{
			LocalizationController.S(Entries.Main.LABEL_OFF),
			LocalizationController.S(Entries.Main.LABEL_ON),
			LocalizationController.S(Entries.Player.MENU_OPTIONS_DISPLAY_DEATH_FEED_OPTION_PROXIMITY)
		}, index)
		{
			Description = LocalizationController.S(Entries.Player.MENU_OPTIONS_DISPLAY_DEATH_FEED_DESC)
		});
		menu7.AddMenuItem(item);
		Menu menu8 = subMenus["display"];
		item = (menuItems["speedometer"] = new MenuListItem(LocalizationController.S(Entries.Player.MENU_OPTIONS_DISPLAY_SPEEDOMETER_TEXT), new List<string>
		{
			LocalizationController.S(Entries.Main.LABEL_OFF),
			LocalizationController.S(Entries.Main.LABEL_ON)
		}, flag2 ? 1 : 0)
		{
			Description = LocalizationController.S(Entries.Player.MENU_OPTIONS_DISPLAY_SPEEDOMETER_DESC)
		});
		menu8.AddMenuItem(item);
		Menu menu9 = subMenus["display"];
		item = (menuItems["altimeter"] = new MenuListItem(LocalizationController.S(Entries.Player.MENU_OPTIONS_DISPLAY_ALTIMETER_TEXT), new List<string>
		{
			LocalizationController.S(Entries.Main.LABEL_OFF),
			LocalizationController.S(Entries.Main.LABEL_ON)
		}, flag3 ? 1 : 0)
		{
			Description = LocalizationController.S(Entries.Player.MENU_OPTIONS_DISPLAY_ALTIMETER_DESC)
		});
		menu9.AddMenuItem(item);
		Menu menu10 = subMenus["display"];
		item = (menuItems["screenshotMode"] = new MenuListItem(LocalizationController.S(Entries.Player.MENU_OPTIONS_DISPLAY_IMMERSIVE_MODE_TEXT), new List<string>
		{
			LocalizationController.S(Entries.Main.LABEL_OFF),
			LocalizationController.S(Entries.Main.LABEL_ON)
		}, 0)
		{
			Description = LocalizationController.S(Entries.Player.MENU_OPTIONS_DISPLAY_IMMERSIVE_MODE_DESC)
		});
		menu10.AddMenuItem(item);
		Menu menu11 = subMenus["display"];
		item = (menuItems["toggleChat"] = new MenuListItem(LocalizationController.S(Entries.Player.MENU_OPTIONS_DISPLAY_TEXT_CHAT_TEXT), new List<string>
		{
			LocalizationController.S(Entries.Main.LABEL_OFF),
			LocalizationController.S(Entries.Main.LABEL_ON)
		}, 1)
		{
			Description = LocalizationController.S(Entries.Player.MENU_OPTIONS_DISPLAY_TEXT_CHAT_DESC)
		});
		menu11.AddMenuItem(item);
		Menu menu12 = subMenus["display"];
		item = (menuItems["propertyBlips"] = new MenuListItem(LocalizationController.S(Entries.Player.MENU_OPTIONS_DISPLAY_PROPERTY_BLIPS_TEXT), new List<string>
		{
			LocalizationController.S(Entries.Main.LABEL_OFF),
			LocalizationController.S(Entries.Player.MENU_OPTIONS_DISPLAY_PROPERTY_BLIPS_OPTION_ALL),
			LocalizationController.S(Entries.Player.MENU_OPTIONS_DISPLAY_PROPERTY_BLIPS_OPTION_OWNED)
		}, index2)
		{
			Description = LocalizationController.S(Entries.Player.MENU_OPTIONS_DISPLAY_PROPERTY_BLIPS_DESC)
		});
		menu12.AddMenuItem(item);
		Menu menu13 = subMenus["display"];
		item = (menuItems["healthPercent"] = new MenuListItem(LocalizationController.S(Entries.Player.MENU_OPTIONS_DISPLAY_HEALTH_PERCENT_TEXT), new List<string>
		{
			LocalizationController.S(Entries.Main.LABEL_OFF),
			LocalizationController.S(Entries.Main.LABEL_ON)
		}, flag4 ? 1 : 0)
		{
			Description = LocalizationController.S(Entries.Player.MENU_OPTIONS_DISPLAY_HEALTH_PERCENT_DESC)
		});
		menu13.AddMenuItem(item);
		Menu menu14 = subMenus["display"];
		item = (menuItems["overheadSigns"] = new MenuListItem(LocalizationController.S(Entries.Player.MENU_OPTIONS_DISPLAY_SIGNS_TEXT), new List<string>
		{
			LocalizationController.S(Entries.Main.LABEL_OFF),
			LocalizationController.S(Entries.Main.LABEL_ON)
		}, flag5 ? 1 : 0)
		{
			Description = LocalizationController.S(Entries.Player.MENU_OPTIONS_DISPLAY_SIGNS_DESC)
		});
		menu14.AddMenuItem(item);
		Menu menu15 = subMenus["display"];
		item = (menuItems["lowFuelPrompt"] = new MenuListItem(LocalizationController.S(Entries.Player.MENU_OPTIONS_DISPLAY_LOW_FUEL_PROMPT_TEXT), new List<string>
		{
			LocalizationController.S(Entries.Main.LABEL_OFF),
			LocalizationController.S(Entries.Main.LABEL_ON)
		}, flag6 ? 1 : 0)
		{
			Description = LocalizationController.S(Entries.Player.MENU_OPTIONS_DISPLAY_LOW_FUEL_PROMPT_DESC)
		});
		menu15.AddMenuItem(item);
		Menu menu16 = subMenus["display"];
		item = (menuItems["fuelBar"] = new MenuListItem(LocalizationController.S(Entries.Player.MENU_OPTIONS_DISPLAY_FUEL_BAR_TEXT), new List<string>
		{
			LocalizationController.S(Entries.Main.LABEL_OFF),
			LocalizationController.S(Entries.Main.LABEL_ON)
		}, flag7 ? 1 : 0)
		{
			Description = LocalizationController.S(Entries.Player.MENU_OPTIONS_DISPLAY_FUEL_BAR_DESC)
		});
		menu16.AddMenuItem(item);
		Menu menu17 = subMenus["display"];
		item = (menuItems["reticle"] = new MenuListItem(LocalizationController.S(Entries.Player.MENU_OPTIONS_DISPLAY_CROSSHAIR_TEXT), new List<string>
		{
			LocalizationController.S(Entries.Main.LABEL_OFF),
			LocalizationController.S(Entries.Main.LABEL_ON)
		}, flag8 ? 1 : 0)
		{
			Description = LocalizationController.S(Entries.Player.MENU_OPTIONS_DISPLAY_CROSSHAIR_DESC)
		});
		menu17.AddMenuItem(item);
		Menu menu18 = subMenus["display"];
		item = (menuItems["blipPlayerTags"] = new MenuListItem(LocalizationController.S(Entries.Player.MENU_OPTIONS_DISPLAY_USERNAMES_ON_BLIPS_TEXT), new List<string>
		{
			LocalizationController.S(Entries.Main.LABEL_OFF),
			LocalizationController.S(Entries.Main.LABEL_ON)
		}, flag9 ? 1 : 0)
		{
			Description = LocalizationController.S(Entries.Player.MENU_OPTIONS_DISPLAY_USERNAMES_ON_BLIPS_DESC)
		});
		menu18.AddMenuItem(item);
		Menu menu19 = subMenus["display"];
		item = (menuItems["hideXPBar"] = new MenuListItem(LocalizationController.S(Entries.Player.MENU_OPTIONS_DISPLAY_HIDE_XP_BAR_TEXT), new List<string>
		{
			LocalizationController.S(Entries.Main.LABEL_OFF),
			LocalizationController.S(Entries.Main.LABEL_ON)
		}, flag10 ? 1 : 0)
		{
			Description = LocalizationController.S(Entries.Player.MENU_OPTIONS_DISPLAY_HIDE_XP_BAR_DESC)
		});
		menu19.AddMenuItem(item);
		Menu menu20 = subMenus["display"];
		item = (menuItems["offsets"] = new MenuItem(LocalizationController.S(Entries.Player.MENU_OPTIONS_DISPLAY_MENU_OFFSETS_TEXT))
		{
			Description = LocalizationController.S(Entries.Player.MENU_OPTIONS_DISPLAY_MENU_OFFSETS_DESC),
			Label = Utils.MENU_ARROW
		});
		menu20.AddMenuItem(item);
		AddSubmenuItem("accessibility", Menu, new Menu(LocalizationController.S(Entries.Player.MENU_OPTIONS_ACCESSIBILITY_TITLE), LocalizationController.S(Entries.Player.MENU_OPTIONS_ACCESSIBILITY_SUBTITLE)), new MenuItem(LocalizationController.S(Entries.Player.MENU_OPTIONS_ACCESSIBILITY_TITLE), LocalizationController.S(Entries.Player.MENU_OPTIONS_ACCESSIBILITY_SUBTITLE))
		{
			LeftIcon = MenuItem.Icon.GTACNR_ACCESSIBILITY
		}, addOpenHandler: false, addSelectHandler: true, addListIndexChangedHandler: true);
		bool flag11 = Preferences.ColorBlindModeEnabled.Get();
		Menu menu21 = subMenus["accessibility"];
		item = (menuItems["colorBlindMode"] = new MenuListItem(LocalizationController.S(Entries.Player.MENU_OPTIONS_ACCESSIBILITY_COLORBLIND_MODE_TEXT), new List<string>
		{
			LocalizationController.S(Entries.Main.LABEL_OFF),
			LocalizationController.S(Entries.Main.LABEL_ON)
		}, flag11 ? 1 : 0)
		{
			Description = LocalizationController.S(Entries.Player.MENU_OPTIONS_ACCESSIBILITY_COLORBLIND_MODE_DESC)
		});
		menu21.AddMenuItem(item);
		bool flag12 = Preferences.FlashbangBlackoutMode.Get();
		Menu menu22 = subMenus["accessibility"];
		item = (menuItems["flashbangMode"] = new MenuListItem(LocalizationController.S(Entries.Player.MENU_OPTIONS_ACCESSIBILITY_FLASHBANG_MODE_TEXT), new List<string>
		{
			LocalizationController.S(Entries.Player.MENU_OPTIONS_ACCESSIBILITY_FLASHBANG_MODE_OPTION_STANDARD),
			LocalizationController.S(Entries.Player.MENU_OPTIONS_ACCESSIBILITY_FLASHBANG_MODE_OPTION_BLACKOUT)
		}, flag12 ? 1 : 0)
		{
			Description = LocalizationController.S(Entries.Player.MENU_OPTIONS_ACCESSIBILITY_FLASHBANG_MODE_DESC)
		});
		menu22.AddMenuItem(item);
		bool flag13 = Preferences.ThousandsSeparator.Get();
		Menu menu23 = subMenus["accessibility"];
		item = (menuItems["thousandsSeparator"] = new MenuListItem(LocalizationController.S(Entries.Player.MENU_OPTIONS_ACCESSIBILITY_THOUSANDS_SEPARATOR_TEXT), new List<string>
		{
			LocalizationController.S(Entries.Main.LABEL_OFF),
			LocalizationController.S(Entries.Main.LABEL_ON)
		}, flag13 ? 1 : 0)
		{
			Description = LocalizationController.S(Entries.Player.MENU_OPTIONS_ACCESSIBILITY_THOUSANDS_SEPARATOR_DESC)
		});
		menu23.AddMenuItem(item);
		AddSubmenuItem("hotkeys", Menu, new Menu(LocalizationController.S(Entries.Player.MENU_OPTIONS_HOTKEYS_TITLE), LocalizationController.S(Entries.Player.MENU_OPTIONS_HOTKEYS_SUBTITLE)), new MenuItem(LocalizationController.S(Entries.Player.MENU_OPTIONS_HOTKEYS_TITLE), LocalizationController.S(Entries.Player.MENU_OPTIONS_HOTKEYS_SUBTITLE_2))
		{
			LeftIcon = MenuItem.Icon.GTACNR_HOTKEYS
		}, addOpenHandler: true, addSelectHandler: true);
		subMenus["hotkeys"].InstructionalButtons.Clear();
		subMenus["hotkeys"].InstructionalButtons.Add((Control)201, LocalizationController.S(Entries.Player.MENU_OPTIONS_KEYBINDS_BTN_BIND));
		subMenus["hotkeys"].InstructionalButtons.Add((Control)202, LocalizationController.S(Entries.Main.BTN_BACK));
		subMenus["hotkeys"].InstructionalButtons.Add((Control)214, LocalizationController.S(Entries.Player.MENU_OPTIONS_KEYBINDS_BTN_UNBIND));
		subMenus["hotkeys"].ButtonPressHandlers.AddRange(new Menu.ButtonPressHandler[1]
		{
			new Menu.ButtonPressHandler((Control)214, MenuAPI.Menu.ControlPressCheckType.JUST_PRESSED, OnKeyUnbind, disableControl: true)
		});
		subMenus["actions"] = new Menu(LocalizationController.S(Entries.Player.MENU_OPTIONS_HOTKEYS_TITLE), LocalizationController.S(Entries.Player.MENU_OPTIONS_HOTKEYS_SUBTITLE_3));
		Menu menu24 = subMenus["actions"];
		item = (menuItems["weapon"] = new MenuItem("Weapon", "Selects a weapon."));
		menu24.AddMenuItem(item);
		Menu menu25 = subMenus["actions"];
		item = (menuItems["item"] = new MenuItem("Item", "Uses an item from your inventory."));
		menu25.AddMenuItem(item);
		Menu menu26 = subMenus["actions"];
		item = (menuItems["command"] = new MenuItem("Command", "Runs a command."));
		menu26.AddMenuItem(item);
		MenuController.AddSubmenu(subMenus["hotkeys"], subMenus["actions"]);
		subMenus["actions"].OnMenuOpen += OnMenuOpen;
		subMenus["actions"].OnItemSelect += OnItemSelect;
		subMenus["weapons"] = new Menu(LocalizationController.S(Entries.Player.MENU_OPTIONS_WEAPONS_TITLE), LocalizationController.S(Entries.Player.MENU_OPTIONS_WEAPONS_SUBTITLE));
		MenuController.AddSubmenu(subMenus["actions"], subMenus["weapons"]);
		MenuController.BindMenuItem(subMenus["actions"], subMenus["weapons"], menuItems["weapon"]);
		subMenus["weapons"].OnMenuOpen += OnMenuOpen;
		subMenus["weapons"].OnItemSelect += OnItemSelect;
		subMenus["items"] = new Menu(LocalizationController.S(Entries.Player.MENU_OPTIONS_ITEMS_TITLE), LocalizationController.S(Entries.Player.MENU_OPTIONS_ITEMS_SUBTITLE));
		MenuController.AddSubmenu(subMenus["actions"], subMenus["items"]);
		MenuController.BindMenuItem(subMenus["actions"], subMenus["items"], menuItems["item"]);
		subMenus["items"].OnMenuOpen += OnMenuOpen;
		subMenus["items"].OnItemSelect += OnItemSelect;
		AddSubmenuItem("gameplay", Menu, new Menu(LocalizationController.S(Entries.Player.MENU_OPTIONS_GAMEPLAY_TITLE), LocalizationController.S(Entries.Player.MENU_OPTIONS_GAMEPLAY_SUBTITLE)), new MenuItem(LocalizationController.S(Entries.Player.MENU_OPTIONS_GAMEPLAY_TITLE), LocalizationController.S(Entries.Player.MENU_OPTIONS_GAMEPLAY_SUBTITLE))
		{
			LeftIcon = MenuItem.Icon.GTACNR_HANDGUNS
		}, addOpenHandler: false, addSelectHandler: true, addListIndexChangedHandler: true);
		Menu menu27 = subMenus["gameplay"];
		item = (menuItems["revenge"] = new MenuItem(LocalizationController.S(Entries.Player.MENU_OPTIONS_GAMEPLAY_REVENGE_TITLE))
		{
			Description = LocalizationController.S(Entries.Player.MENU_OPTIONS_GAMEPLAY_REVENGE_SUBTITLE),
			Label = Utils.MENU_ARROW
		});
		menu27.AddMenuItem(item);
		AddSubmenuItem("misc", Menu, new Menu(LocalizationController.S(Entries.Player.MENU_OPTIONS_MISC_TITLE), LocalizationController.S(Entries.Player.MENU_OPTIONS_MISC_SUBTITLE)), new MenuItem(LocalizationController.S(Entries.Player.MENU_OPTIONS_MISC_TITLE), LocalizationController.S(Entries.Player.MENU_OPTIONS_MISC_SUBTITLE))
		{
			LeftIcon = MenuItem.Icon.GTACNR_OTHER
		}, addOpenHandler: false, addSelectHandler: true, addListIndexChangedHandler: true);
		bool flag14 = Preferences.FlashlightModeEnabled.Get();
		Menu menu28 = subMenus["misc"];
		item = (menuItems["flashMode"] = new MenuListItem(LocalizationController.S(Entries.Player.MENU_OPTIONS_MISC_FLASHLIGHTS_STAY_ON_TEXT), new List<string>
		{
			LocalizationController.S(Entries.Main.LABEL_OFF),
			LocalizationController.S(Entries.Main.LABEL_ON)
		}, flag14 ? 1 : 0)
		{
			Description = LocalizationController.S(Entries.Player.MENU_OPTIONS_MISC_FLASHLIGHTS_STAY_ON_DESC)
		});
		menu28.AddMenuItem(item);
		bool flag15 = Preferences.RadioClicksEnabled.Get();
		Menu menu29 = subMenus["misc"];
		item = (menuItems["radioClicks"] = new MenuListItem(LocalizationController.S(Entries.Player.MENU_OPTIONS_MISC_RADIO_CLICKS_TEXT), new List<string>
		{
			LocalizationController.S(Entries.Main.LABEL_OFF),
			LocalizationController.S(Entries.Main.LABEL_ON)
		}, flag15 ? 1 : 0)
		{
			Description = LocalizationController.S(Entries.Player.MENU_OPTIONS_MISC_RADIO_CLICKS_DESC)
		});
		menu29.AddMenuItem(item);
		int index3 = (int)Preferences.CameraCycleMode.Get();
		Menu menu30 = subMenus["misc"];
		item = (menuItems["camCycleMode"] = new MenuListItem(LocalizationController.S(Entries.Player.MENU_OPTIONS_MISC_CAM_CYCLE_TEXT), new List<string>
		{
			LocalizationController.S(Entries.Player.MENU_OPTIONS_MISC_CAM_CYCLE_NORMAL),
			LocalizationController.S(Entries.Player.MENU_OPTIONS_MISC_CAM_CYCLE_FAR_FP),
			LocalizationController.S(Entries.Player.MENU_OPTIONS_MISC_CAM_CYCLE_MEDIUM_FP),
			LocalizationController.S(Entries.Player.MENU_OPTIONS_MISC_CAM_CYCLE_NEAR_FP),
			LocalizationController.S(Entries.Player.MENU_OPTIONS_MISC_CAM_CYCLE_NO_FP)
		}, index3)
		{
			Description = LocalizationController.S(Entries.Player.MENU_OPTIONS_MISC_CAM_CYCLE_DESC)
		});
		menu30.AddMenuItem(item);
		Menu.OnMenuOpen += OnMenuOpen;
		Menu.OnItemSelect += OnItemSelect;
	}

	private void OnPurchaseableItemsMenuOpen(Menu menu)
	{
		if (menu.ParentMenu == subMenus["account"])
		{
			PurchaseableItemsMenuScript.SetDefaultValues();
		}
	}

	private async Task RefreshLinkedServices()
	{
		if (isRefreshingLinkedServices)
		{
			return;
		}
		isRefreshingLinkedServices = true;
		try
		{
			string text = await TriggerServerEventAsync<string>("gtacnr:auth:getLinkedServices", new object[0]);
			if (string.IsNullOrEmpty(text))
			{
				return;
			}
			linkedServices = text.Unjson<Dictionary<string, string>>();
			subMenus["services"].ClearMenuItems();
			API.SetHudColour(192, 88, 101, 242, 255);
			API.SetHudColour(193, 249, 117, 26, 255);
			API.SetHudColour(194, 102, 192, 244, 255);
			string text2 = null;
			foreach (string key in enabledServices.Keys)
			{
				AccountServiceInfo accountServiceInfo = enabledServices[key];
				MenuItem.Icon rightIcon = MenuItem.Icon.NONE;
				MenuItem.Icon icon = accountServiceInfo.Icon;
				string description;
				string label;
				if (linkedServices.ContainsKey(key))
				{
					if (text2 == null)
					{
						text2 = key;
					}
					description = $"Your {accountServiceInfo} account is linked to CNRV. Select this item to unlink it.";
					label = linkedServices[key];
					if (text2 == key)
					{
						rightIcon = MenuItem.Icon.INV_MISSION;
					}
				}
				else
				{
					description = $"You did not link your {accountServiceInfo} account to CNRV. Select this item to link it.";
					label = "~c~NOT LINKED";
				}
				Menu menu = subMenus["services"];
				MenuItem item = (menuItems[key] = new MenuItem(accountServiceInfo.ToString(), description)
				{
					Label = label,
					LeftIcon = icon,
					RightIcon = rightIcon,
					ItemData = key
				});
				menu.AddMenuItem(item);
			}
		}
		catch (Exception exception)
		{
			Print(exception);
		}
		finally
		{
			isRefreshingLinkedServices = false;
		}
	}

	private async Task RefreshSubscriptions()
	{
		if (isRefreshingSubscriptions)
		{
			return;
		}
		isRefreshingSubscriptions = true;
		try
		{
			List<MembershipEntry> list = (await TriggerServerEventAsync<string>("gtacnr:memberships:getAllSubscriptions", new object[0])).Unjson<List<MembershipEntry>>();
			StaffLevel staffLevel = StaffLevelScript.StaffLevel;
			if ((int)staffLevel >= 110)
			{
				list.Insert(0, new MembershipEntry
				{
					Tier = MembershipTier.Gold,
					IsTemporary = true
				});
			}
			else if ((int)staffLevel >= 10)
			{
				list.Insert(0, new MembershipEntry
				{
					Tier = MembershipTier.Silver,
					IsTemporary = true
				});
			}
			subMenus["subscriptions"].ClearMenuItems();
			if (list == null)
			{
				menuItems["subscriptions"].Enabled = false;
				menuItems["subscriptions"].Label = "~r~ERROR";
				menuItems["subscriptions"].Description = LocalizationController.S(Entries.Main.UNEXPECTED_ERROR) + " " + LocalizationController.S(Entries.Main.ERROR_LOADING_SUBSCRIPTION_DATA);
				return;
			}
			if (list.Count == 0)
			{
				menuItems["subscriptions"].Enabled = false;
				menuItems["subscriptions"].Label = "NONE";
				menuItems["subscriptions"].Description = "You don't have any ~p~subscription ~s~history. Visit ~b~" + ExternalLinks.Collection.Store + " ~s~for more information.";
				return;
			}
			string description = Gtacnr.Utils.GetDescription(list.FirstOrDefault((MembershipEntry s) => s.Status == MembershipStatus.Active || s.IsTemporary)?.Tier ?? MembershipTier.None);
			menuItems["subscriptions"].Enabled = true;
			menuItems["subscriptions"].Label = "~p~" + description.ToUpperInvariant();
			menuItems["subscriptions"].Description = "View all your ~p~subscription ~s~history.";
			foreach (MembershipEntry item3 in list)
			{
				string description2 = Gtacnr.Utils.GetDescription(item3.Tier);
				string text = (item3.IsTemporary ? "~g~" : ((item3.Status == MembershipStatus.Active) ? "~p~" : ((item3.Status == MembershipStatus.Expired) ? "~c~" : ((item3.Status == MembershipStatus.Future) ? "" : ""))));
				if (item3.IsTemporary)
				{
					MenuItem item = new MenuItem(text + description2)
					{
						Description = "Tier: ~p~" + description2 + "~s~\n~g~Free For Staff Members",
						Label = "~g~STAFF"
					};
					subMenus["subscriptions"].AddMenuItem(item);
					continue;
				}
				string text2 = item3.StartDate.ToFormalDate2();
				string text3 = item3.ExpiryDate.ToFormalDate2();
				MenuItem menuItem = new MenuItem(text + description2);
				menuItem.Description = "Tier: ~p~" + description2 + "~s~\nStart date: ~p~" + text2 + "~s~\nExpiry date: ~p~" + text3;
				menuItem.Label = text + text2;
				MenuItem item2 = menuItem;
				subMenus["subscriptions"].AddMenuItem(item2);
			}
		}
		catch (Exception exception)
		{
			Print(exception);
		}
		finally
		{
			isRefreshingSubscriptions = false;
		}
	}

	private void DisableLinkedServiceItems()
	{
		foreach (MenuItem menuItem in subMenus["services"].GetMenuItems())
		{
			menuItem.Enabled = false;
		}
	}

	private void EnableLinkedServiceItems()
	{
		foreach (MenuItem menuItem in subMenus["services"].GetMenuItems())
		{
			menuItem.Enabled = true;
		}
	}

	private void AddSubmenuItem(string key, Menu parentMenu, Menu subMenu, MenuItem menuItem, bool addOpenHandler = false, bool addSelectHandler = false, bool addListIndexChangedHandler = false)
	{
		subMenus[key] = subMenu;
		menuItems[key] = menuItem;
		parentMenu.AddMenuItem(menuItem);
		MenuController.AddSubmenu(parentMenu, subMenu);
		MenuController.BindMenuItem(parentMenu, subMenu, menuItem);
		if (addOpenHandler)
		{
			subMenus[key].OnMenuOpen += OnMenuOpen;
		}
		if (addSelectHandler)
		{
			subMenus[key].OnItemSelect += OnItemSelect;
		}
		if (addListIndexChangedHandler)
		{
			subMenus[key].OnListIndexChange += OnMenuListIndexChange;
		}
	}

	private async void OnMenuOpen(Menu menu)
	{
		if (menu == subMenus["hotkeys"])
		{
			int currentIndex = menu.CurrentIndex;
			RefreshHotkeysMenu();
			menu.CurrentIndex = currentIndex;
			return;
		}
		if (menu == subMenus["actions"])
		{
			menu.CurrentIndex = 0;
			return;
		}
		if (menu == subMenus["weapons"])
		{
			menu.ClearMenuItems();
			{
				foreach (WeaponDefinition allWeaponDefinition in Gtacnr.Data.Items.GetAllWeaponDefinitions())
				{
					if (Game.PlayerPed.Weapons.HasWeapon((WeaponHash)API.GetHashKey(allWeaponDefinition.Id)))
					{
						menu.AddMenuItem(new MenuItem(allWeaponDefinition.Name, allWeaponDefinition.Description)
						{
							ItemData = allWeaponDefinition.Id
						});
					}
				}
				return;
			}
		}
		if (menu != subMenus["items"])
		{
			return;
		}
		menu.ClearMenuItems();
		menu.AddLoadingMenuItem();
		IEnumerable<InventoryEntry> enumerable = InventoryMenuScript.Cache;
		if (enumerable == null)
		{
			enumerable = await InventoryMenuScript.ReloadInventory();
		}
		menu.ClearMenuItems();
		foreach (string item in from e in enumerable
			where Gtacnr.Data.Items.GetItemDefinition(e.ItemId).CanUse
			orderby Gtacnr.Data.Items.GetItemDefinition(e.ItemId).Category, e.Position
			select e.ItemId)
		{
			InventoryItem itemDefinition = Gtacnr.Data.Items.GetItemDefinition(item);
			if (itemDefinition != null)
			{
				menu.AddMenuItem(new MenuItem(itemDefinition.Name, itemDefinition.Description)
				{
					ItemData = itemDefinition.Id
				});
			}
		}
	}

	private async void OnItemSelect(Menu menu, MenuItem menuItem, int itemIndex)
	{
		if (IsMenuSelected("hotkeys"))
		{
			if (menu.GetCurrentMenuItem().ItemData is string text)
			{
				currentHotkey = text;
			}
		}
		else if (IsMenuSelected("weapons"))
		{
			if (menuItem.ItemData is string param)
			{
				KeybindScript.SetBind(currentHotkey, new BindableAction
				{
					Type = BindableActionType.EquipWeapon,
					Param = param
				});
				RefreshHotkeysMenu();
				MenuController.CloseAllMenus();
				subMenus["hotkeys"].Visible = true;
			}
		}
		else if (IsMenuSelected("items"))
		{
			if (menuItem.ItemData is string param2)
			{
				KeybindScript.SetBind(currentHotkey, new BindableAction
				{
					Type = BindableActionType.UseItem,
					Param = param2
				});
				RefreshHotkeysMenu();
				MenuController.CloseAllMenus();
				subMenus["hotkeys"].Visible = true;
			}
		}
		else if (IsMenuSelected("services"))
		{
			DisableLinkedServiceItems();
			try
			{
				string serviceId = menu.GetCurrentMenuItem().ItemData as string;
				AccountServiceInfo serviceInfo = enabledServices[serviceId];
				LinkIdentifierResponse res;
				if (linkedServices.ContainsKey(serviceId))
				{
					if (linkedServices.Count == 1)
					{
						Utils.DisplayHelpText("~r~You cannot unlink all services. There must be at least one service linked.");
						return;
					}
					if (!(await Utils.ShowConfirm($"Do you really want to unlink {serviceInfo} from your account?", "unlink " + serviceId)))
					{
						return;
					}
					LinkIdentifierResponse linkIdentifierResponse = (LinkIdentifierResponse)(await TriggerServerEventAsync<int>("gtacnr:auth:unlinkService", new object[1] { serviceId }));
					switch (linkIdentifierResponse)
					{
					case LinkIdentifierResponse.Success:
						Utils.DisplayHelpText($"You ~g~successfully ~s~unlinked {serviceInfo} from your account.");
						await RefreshLinkedServices();
						break;
					case LinkIdentifierResponse.Busy:
						Utils.DisplayHelpText("~r~The server is still processing your previous request. Please, wait before sending another one.");
						break;
					case LinkIdentifierResponse.FakeUsername:
						Utils.DisplayHelpText("~r~You cannot do this while you're using a fake username.");
						break;
					default:
						Utils.DisplayHelpText(LocalizationController.S(Entries.Main.UNEXPECTED_ERROR_CODE, $"0x23-{(int)linkIdentifierResponse}"));
						break;
					}
				}
				else
				{
					res = (LinkIdentifierResponse)(await TriggerServerEventAsync<int>("gtacnr:auth:linkService", new object[2] { serviceId, false }));
					await ProcessResponse();
				}
				async Task ProcessResponse()
				{
					switch (res)
					{
					case LinkIdentifierResponse.Success:
						Utils.DisplayHelpText($"You ~g~successfully ~s~linked {serviceInfo} to your account.");
						await RefreshLinkedServices();
						break;
					case LinkIdentifierResponse.Busy:
						Utils.DisplayHelpText("~r~The server is still processing your previous request. Please, wait before sending another one.");
						break;
					case LinkIdentifierResponse.FakeUsername:
						Utils.DisplayHelpText("~r~You cannot do this while you're using a fake username.");
						break;
					case LinkIdentifierResponse.EmptyIdentifier:
						switch (serviceId)
						{
						case "discord":
						case "steam":
							Utils.DisplayHelpText($"{serviceInfo} account not found. Make sure your {serviceInfo} desktop app is running, then restart FiveM and retry.");
							break;
						case "fivem":
							Utils.DisplayHelpText($"{serviceInfo} account not found. Disconnect and go back to the FiveM launcher, then relog in to your FiveM account and retry.");
							break;
						}
						break;
					case LinkIdentifierResponse.AlreadyLinked:
						if (await Utils.ShowConfirm($"This {serviceInfo} is already linked to another account. Do you want to unlink it from that account and link it to this one? " + "Be advised that you may not be able to access that account anymore if there are no more 3rd party auth services linked to it.", serviceId + " already linked"))
						{
							res = (LinkIdentifierResponse)(await TriggerServerEventAsync<int>("gtacnr:auth:linkService", new object[2] { serviceId, true }));
							await ProcessResponse();
						}
						break;
					default:
						Utils.DisplayHelpText(LocalizationController.S(Entries.Main.UNEXPECTED_ERROR_CODE, $"0x24-{(int)res}"));
						break;
					}
				}
			}
			catch (Exception exception)
			{
				Print(exception);
				Utils.DisplayHelpText(LocalizationController.S(Entries.Main.UNEXPECTED_ERROR));
			}
			finally
			{
				EnableLinkedServiceItems();
			}
		}
		if (IsItemSelected("command"))
		{
			string text2 = await Utils.GetUserInput("Command", "Enter the command you want to bind", "", 100);
			if (!string.IsNullOrWhiteSpace(text2))
			{
				text2 = text2.TrimStart('/').Trim();
				KeybindScript.SetBind(currentHotkey, new BindableAction
				{
					Type = BindableActionType.Custom,
					Param = text2
				});
				RefreshHotkeysMenu();
				MenuController.CloseAllMenus();
				subMenus["hotkeys"].Visible = true;
			}
		}
		else if (IsItemSelected("refreshMember"))
		{
			menuItems["refreshMember"].Enabled = false;
			menuItems["refreshMember"].Label = "...";
			await MembershipScript.Refresh();
			menuItems["refreshMember"].Enabled = true;
			menuItems["refreshMember"].Label = null;
		}
		else if (IsItemSelected("changeName"))
		{
			bool num = (int)MembershipScript.GetCurrentMembershipTier() > 0;
			bool flag = PurchaseableItemsMenuScript.PurchasedItems.Any((PurchaseableEntry i) => i.ItemId == "name_change_token" || i.ItemId == "name_change_token_nt");
			if (!num && !flag)
			{
				Utils.DisplayHelpText("~r~You need to purchase a ~s~Name Change Token ~r~or become a ~s~Premium Member ~r~to change your username by yourself.Visit ~s~" + ExternalLinks.Collection.Store + " ~r~for more info. If you only want to fix a typo, you can get it for ~g~free ~s~by opening a support ticket.");
				return;
			}
			string newName = await Utils.GetUserInput("Username", "Enter your desired username (max 24 characters)", "", 24);
			if (string.IsNullOrWhiteSpace(newName))
			{
				Utils.PlayErrorSound();
				Utils.DisplayHelpText("~r~Operation canceled.", playSound: false);
				return;
			}
			if (!(await Utils.ShowConfirm("Do you really want to change your name to ~y~" + newName + "~s~?\n\nBy using this feature, you agree not to ~r~impersonate ~s~any other player, pretend being part of a ~r~well-known crew~s~, or try to confuse the server management, and that breaking this agreement equates to ~r~lying to staff ~s~which is a ~r~bannable ~s~offense. All name changes are recorded and we may enforce this rule at any time in the future.", "Name change", TimeSpan.FromSeconds(10.0))))
			{
				return;
			}
			ChangeNameResponse changeNameResponse = (ChangeNameResponse)(await TriggerServerEventAsync<int>("gtacnr:auth:canChangeName", new object[0]));
			switch (changeNameResponse)
			{
			case ChangeNameResponse.WillUseToken:
				if (!(await Utils.ShowConfirm("A ~p~Name Change Token ~s~will be used because you have changed your name ~r~too recently~s~. Do you want to proceed?", "Warning", TimeSpan.FromSeconds(5.0))))
				{
					return;
				}
				break;
			case ChangeNameResponse.Cooldown:
				return;
			default:
				Utils.DisplayErrorMessage(25, (int)changeNameResponse);
				return;
			case ChangeNameResponse.Success:
				break;
			}
			ChangeNameResponse changeNameResponse2 = (ChangeNameResponse)(await TriggerServerEventAsync<int>("gtacnr:auth:changeName", new object[1] { newName }));
			switch (changeNameResponse2)
			{
			case ChangeNameResponse.Success:
				PurchaseableItemsMenuScript.Instance.RefreshPurchases();
				break;
			case ChangeNameResponse.TooManyRequests:
				Utils.DisplayHelpText("You need to ~r~wait ~s~before changing your ~b~username ~s~again.");
				break;
			case ChangeNameResponse.Taken:
				Utils.DisplayHelpText("The name ~b~" + newName + " ~s~is already taken.");
				break;
			case ChangeNameResponse.StaffRename:
				Utils.DisplayHelpText("~g~Staff members ~s~must contact an admin or manager for a ~b~name change~s~. The admin must make sure that the in-game name matches the name on Discord and is not obscene or vulgar.");
				break;
			default:
				Utils.DisplayErrorMessage(32, (int)changeNameResponse2);
				break;
			case ChangeNameResponse.InvalidName:
			case ChangeNameResponse.Cooldown:
				break;
			}
		}
		else if (IsItemSelected("changeEmail"))
		{
			string newName = await Utils.GetUserInput("Email Address", "Enter your email address.", "", 64, "email");
			if (string.IsNullOrWhiteSpace(newName))
			{
				Utils.PlayErrorSound();
				Utils.DisplayHelpText("~r~Operation canceled.", playSound: false);
				return;
			}
			LinkEmailResponse linkEmailResponse = (LinkEmailResponse)(await TriggerServerEventAsync<int>("gtacnr:auth:linkEmail", new object[1] { newName }));
			switch (linkEmailResponse)
			{
			case LinkEmailResponse.Success:
			{
				string text3;
				int result;
				do
				{
					text3 = await Utils.GetUserInput("Enter OTP", "We've sent you an email to " + newName + " containing a 6-digit code. If you can't find it, make sure to check your spam folder. The OTP expires in 30 minutes.", "", 6, "number");
					int.TryParse(text3, out result);
				}
				while (result < 100000 && !string.IsNullOrWhiteSpace(text3));
				if (result >= 100000)
				{
					VerifyEmailResponse verifyEmailResponse = (VerifyEmailResponse)(await TriggerServerEventAsync<int>("gtacnr:auth:endLinkEmail", new object[1] { result }));
					switch (verifyEmailResponse)
					{
					case VerifyEmailResponse.Success:
						Utils.DisplayHelpText("You've ~g~successfully ~s~linked ~b~" + newName + " ~s~to your account.");
						break;
					case VerifyEmailResponse.IncorrectCode:
						Utils.DisplayHelpText("~r~The code you entered is incorrect.", playSound: false);
						break;
					case VerifyEmailResponse.ExpiredCode:
						Utils.DisplayHelpText("~r~The code has expired.", playSound: false);
						break;
					case VerifyEmailResponse.AlreadyRegistered:
						Utils.DisplayHelpText("~r~This email address is already linked to another account.", playSound: false);
						break;
					default:
						Utils.DisplayHelpText(LocalizationController.S(Entries.Main.UNEXPECTED_ERROR_CODE, $"0x26-{(int)verifyEmailResponse}"));
						break;
					}
				}
				else
				{
					Utils.PlayErrorSound();
					BaseScript.TriggerServerEvent("gtacnr:auth:cancelLinkEmail", new object[0]);
					Utils.DisplayHelpText("~r~Operation canceled.", playSound: false);
				}
				break;
			}
			case LinkEmailResponse.Cooldown:
				Utils.DisplayHelpText("~r~You must wait 2 minutes between email change attempts.");
				break;
			case LinkEmailResponse.InvalidEmail:
				Utils.DisplayHelpText("~r~" + newName + " is not a valid email address.");
				break;
			case LinkEmailResponse.Busy:
				Utils.DisplayHelpText("~r~The server is still processing your previous request. Please, wait before sending another one.");
				break;
			case LinkEmailResponse.FakeUsername:
				Utils.DisplayHelpText("~r~You cannot do this while you're using a fake username.");
				break;
			default:
				Utils.DisplayHelpText(LocalizationController.S(Entries.Main.UNEXPECTED_ERROR_CODE, $"0x25-{(int)linkEmailResponse}"));
				break;
			}
		}
		else if (IsItemSelected("changePassword"))
		{
			string newName = await Utils.GetUserInput("New Password", "Enter a new password.", "", 64, "password");
			if (string.IsNullOrWhiteSpace(newName))
			{
				Utils.PlayErrorSound();
				Utils.DisplayHelpText("~r~Operation canceled.", playSound: false);
				return;
			}
			if (newName.Length < 8)
			{
				Utils.DisplayHelpText("~r~The entered password is too short (min 8 characters). Operation canceled.", playSound: false);
				return;
			}
			string text4 = await Utils.GetUserInput("New Password (Repeat)", "Repeat the password you just entered.", "", 64, "password");
			if (string.IsNullOrWhiteSpace(text4))
			{
				Utils.PlayErrorSound();
				Utils.DisplayHelpText("~r~Operation canceled.", playSound: false);
				return;
			}
			if (newName != text4)
			{
				Utils.PlayErrorSound();
				Utils.DisplayHelpText("~r~The entered passwords don't match. Operation canceled.", playSound: false);
				return;
			}
			ChangePasswordResponse changePasswordResponse = (ChangePasswordResponse)(await TriggerServerEventAsync<int>("gtacnr:auth:changePassword", new object[1] { newName }));
			switch (changePasswordResponse)
			{
			case ChangePasswordResponse.Success:
				Utils.DisplayHelpText("Your ~b~password ~s~has been ~g~successfully changed~s~.", playSound: false);
				break;
			case ChangePasswordResponse.Busy:
				Utils.DisplayHelpText("~r~The server is still processing your previous request. Please, wait before sending another one.");
				break;
			case ChangePasswordResponse.FakeUsername:
				Utils.DisplayHelpText("~r~You cannot do this while you're using a fake username.");
				break;
			default:
				Utils.DisplayHelpText(LocalizationController.S(Entries.Main.UNEXPECTED_ERROR_CODE, $"0x27-{(int)changePasswordResponse}"));
				break;
			}
		}
		if (IsItemSelected("offsets"))
		{
			CustomHUDOffsetScript.OpenMenu(menu);
		}
		if (IsItemSelected("revenge"))
		{
			DeathScript.OpenRevengeMenu(menu);
		}
		bool IsItemSelected(string key)
		{
			if (menuItems.ContainsKey(key))
			{
				return menuItem == menuItems[key];
			}
			return false;
		}
		bool IsMenuSelected(string key)
		{
			if (subMenus.ContainsKey(key))
			{
				return menu == subMenus[key];
			}
			return false;
		}
	}

	private void RefreshHotkeysMenu()
	{
		IReadOnlyDictionary<string, BindableAction> allBinds = KeybindScript.GetAllBinds();
		Menu menu = subMenus["hotkeys"];
		menu.ClearMenuItems();
		for (int i = 0; i < 10; i++)
		{
			string text = $"kb_{i}";
			MenuItem menuItem = new MenuItem($"Hotkey #{i + 1}")
			{
				Label = "~c~Unbound",
				Description = "Configure your hotkeys in the ~b~GTA Settings ~s~> ~b~Key Bindings ~s~> ~b~FiveM ~s~menu.\n~y~NEW! ~s~If you are a controller user, press LS and RS at the same time to open the Quick Actions menu.",
				ItemData = text
			};
			menu.AddMenuItem(menuItem);
			MenuController.BindMenuItem(subMenus["hotkeys"], subMenus["actions"], menuItem);
			if (allBinds.ContainsKey(text))
			{
				BindableAction bindableAction = allBinds[text];
				string description = Gtacnr.Utils.GetDescription(bindableAction.Type);
				string text2 = "";
				if (bindableAction.Type == BindableActionType.EquipWeapon)
				{
					text2 = Gtacnr.Data.Items.GetWeaponDefinition(bindableAction.Param)?.Name ?? "~r~invalid";
				}
				else if (bindableAction.Type == BindableActionType.UseItem)
				{
					text2 = Gtacnr.Data.Items.GetItemDefinition(bindableAction.Param)?.Name ?? "~r~invalid";
				}
				else if (bindableAction.Type == BindableActionType.Custom)
				{
					text2 = "/" + bindableAction.Param;
				}
				menuItem.Label = description + " ~b~" + text2;
			}
		}
	}

	private void OnKeyUnbind(Menu menu, Control control)
	{
		if (menu.GetCurrentMenuItem().ItemData is string key)
		{
			int currentIndex = menu.CurrentIndex;
			KeybindScript.ResetBind(key);
			RefreshHotkeysMenu();
			menu.CurrentIndex = currentIndex;
			Utils.PlaySelectSound();
		}
	}

	private void OnMenuListIndexChange(Menu menu, MenuListItem menuItem, int oldSelectionIndex, int newSelectionIndex, int itemIndex)
	{
		bool flag = newSelectionIndex == 1;
		if (menuItem == menuItems["speedometer"])
		{
			Preferences.SpeedometerEnabled.Set(flag);
			SpeedometerScript.IsSpeedometerEnabled = flag;
		}
		else if (menuItem == menuItems["altimeter"])
		{
			Preferences.AltimeterEnabled.Set(flag);
			AltimeterScript.IsAltimeterEnabled = flag;
		}
		else if (menuItem == menuItems["screenshotMode"])
		{
			HideHUDScript.ScreenshotMode = flag;
		}
		else if (menuItem == menuItems["toggleChat"])
		{
			HideHUDScript.ToggleChat(flag);
		}
		else if (menuItem == menuItems["flashMode"])
		{
			Preferences.FlashlightModeEnabled.Set(flag);
			API.SetFlashLightKeepOnWhileMoving(flag);
		}
		else if (menuItem == menuItems["radioClicks"])
		{
			Preferences.RadioClicksEnabled.Set(flag);
			((dynamic)((BaseScript)this).Exports["pma-voice"]).setVoiceProperty("micClicks", flag);
		}
		else if (menuItem == menuItems["camCycleMode"])
		{
			Preferences.CameraCycleMode.Set((CameraCycleMode)newSelectionIndex);
			WeaponBehaviorScript.CameraCycleMode = (CameraCycleMode)newSelectionIndex;
		}
		else if (menuItem == menuItems["colorBlindMode"])
		{
			Preferences.ColorBlindModeEnabled.Set(flag);
			RadarBlipsScript.ColorBlindMode = flag;
		}
		else if (menuItem == menuItems["flashbangMode"])
		{
			Preferences.FlashbangBlackoutMode.Set(flag);
			FlashBangScript.BlackoutMode = flag;
		}
		else if (menuItem == menuItems["thousandsSeparator"])
		{
			Preferences.ThousandsSeparator.Set(flag);
			BaseScript.TriggerEvent("gtacnr:hud:toggleThousandsSeparator", new object[1] { flag });
		}
		else if (menuItem == menuItems["propertyBlips"])
		{
			Preferences.PropertyBlipsMode.Set(newSelectionIndex);
			BaseScript.TriggerEvent("gtacnr:propertyBlipsToggled", new object[1] { newSelectionIndex });
		}
		else if (menuItem == menuItems["healthPercent"])
		{
			Preferences.HealthPercentEnabled.Set(flag);
			GamerTagsScript.RenderHealthPercentage = flag;
		}
		else if (menuItem == menuItems["overheadSigns"])
		{
			Preferences.OverheadSignsEnabled.Set(flag);
			GamerTagsScript.RenderOverheadSigns = flag;
		}
		else if (menuItem == menuItems["lowFuelPrompt"])
		{
			Preferences.LowFuelPrompt.Set(flag);
			GasScript.IsFuelPromptEnabled = flag;
		}
		else if (menuItem == menuItems["fuelBar"])
		{
			Preferences.AlwaysShowFuelBar.Set(flag);
			GasScript.AlwaysShowFuelBar = flag;
		}
		else if (menuItem == menuItems["reticle"])
		{
			Preferences.ReticleEnabled.Set(flag);
			HideHUDScript.EnableReticle = flag;
		}
		else if (menuItem == menuItems["blipPlayerTags"])
		{
			Preferences.ShowPlayerNameTagsOnBlips.Set(flag);
			HideHUDScript.ShowPlayerNameTagsOnBlips = flag;
		}
		else if (menuItem == menuItems["hideXPBar"])
		{
			Preferences.XPBarHidden.Set(flag);
			XPDisplayScript.XPBarHidden = flag;
		}
		else if (menuItem == menuItems["toggleMenuPos"])
		{
			bool flag2 = flag;
			Preferences.MenusLeftAligned.Set(flag2);
			MenuController.MenuAlignment = ((!flag2) ? MenuController.MenuAlignmentOption.Right : MenuController.MenuAlignmentOption.Left);
			BaseScript.TriggerEvent("gtacnr:hudLayoutToggled", new object[1] { flag2 });
		}
		else if (menuItem == menuItems["deathFeed"])
		{
			Preferences.DeathFeedMode.Set((DeathFeedMode)newSelectionIndex);
			DeathScript.DeathFeedMode = (DeathFeedMode)newSelectionIndex;
		}
		else if (menuItem == menuItems["locale"])
		{
			if (LocalizationController.CurrentLanguage != null)
			{
				LocalizationController.Unload(LocalizationController.CurrentLanguage);
			}
			string text = supportedLocales.ElementAt(newSelectionIndex).Key;
			string value = supportedLocales.ElementAt(newSelectionIndex).Value;
			if (text == "")
			{
				text = LocalizationController.GetLocaleFromGTALanguage();
			}
			LocalizationController.Load(text);
			LocalizationController.CurrentLanguage = text;
			Utils.DisplayHelpText(LocalizationController.S(Entries.Main.LANGUAGE_CHANGED, value));
			Utils.SendNotification(LocalizationController.S(Entries.Main.LANGUAGE_CHANGED_WARNING));
			Preferences.PreferredLanguage.Set(LocalizationController.CurrentLanguage);
			BaseScript.TriggerServerEvent("gtacnr:setLocale", new object[1] { text });
		}
	}

	[EventHandler("gtacnr:screenshotModeChanged")]
	private void OnScreenshotModeChanged()
	{
		(menuItems["screenshotMode"] as MenuListItem).ListIndex = (HideHUDScript.ScreenshotMode ? 1 : 0);
		(menuItems["toggleChat"] as MenuListItem).ListIndex = (HideHUDScript.EnableChat ? 1 : 0);
	}
}
