using System.Collections.Generic;
using System.Linq;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Gtacnr.Client.API;
using Gtacnr.Client.API.Scripts;
using Gtacnr.Client.API.UI.Menus;
using Gtacnr.Client.Businesses;
using Gtacnr.Client.Characters.Editor;
using Gtacnr.Client.Communication;
using Gtacnr.Client.Crimes;
using Gtacnr.Client.IMenu;
using Gtacnr.Client.Jobs.Hitman;
using Gtacnr.Data;
using Gtacnr.Localization;
using Gtacnr.Model;
using MenuAPI;

namespace Gtacnr.Client.Phone;

public class PhoneMenuScript : Script
{
	private Menu phoneMenu;

	private Menu serviceMenu;

	private readonly Dictionary<string, MenuItem> menuItems = new Dictionary<string, MenuItem>();

	private static PhoneMenuScript instance;

	private const string DEFAULT_PHONE_NAME = "iFruit 4";

	public static Menu Menu => instance.phoneMenu;

	public static Menu ServiceMenu => instance.serviceMenu;

	public static bool IsCalling { get; private set; }

	public PhoneMenuScript()
	{
		instance = this;
	}

	protected override async void OnStarted()
	{
		while (MainMenuScript.MainMenu == null || PartyScript.Menu == null)
		{
			await BaseScript.Delay(0);
		}
		phoneMenu = new Menu(LocalizationController.S(Entries.Imenu.IMENU_PHONE), "");
		MenuController.BindMenuItem(MainMenuScript.MainMenu, phoneMenu, MainMenuScript.MainMenuItems["phone"]);
		Menu menu = phoneMenu;
		MenuItem item = (menuItems["services"] = new MenuItem(LocalizationController.S(Entries.Imenu.IMENU_SERVICES), LocalizationController.S(Entries.Imenu.IMENU_SERVICES_DESCR)));
		menu.AddMenuItem(item);
		Menu menu2 = phoneMenu;
		item = (menuItems["camera"] = new MenuItem(LocalizationController.S(Entries.Imenu.IMENU_PHONE_CAMERA), LocalizationController.S(Entries.Imenu.IMENU_PHONE_CAMERA_DESCR)));
		menu2.AddMenuItem(item);
		Menu menu3 = phoneMenu;
		item = (menuItems["party"] = new MenuItem(LocalizationController.S(Entries.Imenu.IMENU_PARTY), LocalizationController.S(Entries.Imenu.IMENU_PARTY_DESCR)));
		menu3.AddMenuItem(item);
		Menu menu4 = phoneMenu;
		item = (menuItems["dm"] = new MenuItem(LocalizationController.S(Entries.Imenu.IMENU_PHONE_SEND_DM), LocalizationController.S(Entries.Imenu.IMENU_PHONE_SEND_DM_DESCR)));
		menu4.AddMenuItem(item);
		serviceMenu = new Menu(LocalizationController.S(Entries.Imenu.IMENU_SERVICES), LocalizationController.S(Entries.Imenu.IMENU_SERVICES_DESCR));
		MenuController.BindMenuItem(phoneMenu, serviceMenu, menuItems["services"]);
		MenuController.BindMenuItem(phoneMenu, PartyScript.Menu, menuItems["party"]);
		phoneMenu.OnItemSelect += OnItemSelect;
		serviceMenu.OnItemSelect += OnItemSelect;
		MainMenuScript.MainMenu.OnItemSelect += OnItemSelect;
		KeysScript.AttachListener((Control)27, OnKeyEvent, 20);
	}

	private bool OnKeyEvent(Control control, KeyEventType eventType, InputType inputType)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Invalid comparison between Unknown and I4
		if (eventType == KeyEventType.JustPressed && inputType == InputType.Keyboard && (int)control == 27)
		{
			if (PhoneCameraScript.IsCameraOpen() || MenuController.IsAnyMenuOpen() || CharacterCreationScript.IsInCreator)
			{
				return false;
			}
			MenuController.CloseAllMenus();
			phoneMenu.OpenMenu();
			OnPhoneOpened();
			return true;
		}
		return false;
	}

	private void OnPhoneOpened()
	{
		RefreshPhone();
		RefreshServices();
		PartyScript.RefreshPartyMenu();
		HoldPhone();
	}

	private async void HoldPhone()
	{
		PhoneScript.OpenPhone();
		do
		{
			await BaseScript.Delay(50);
		}
		while (IsPhoneMenuOpen() || PhoneCameraScript.IsCameraOpen());
		if (!IsCalling)
		{
			PhoneScript.ClosePhone();
		}
	}

	public static async void CallAnim()
	{
		IsCalling = true;
		PhoneScript.PlayPhoneAnim("cellphone_text_to_call");
		await BaseScript.Delay(3000);
		PhoneScript.ClosePhone();
		IsCalling = false;
	}

	private void OnItemSelect(Menu menu, MenuItem menuItem, int itemIndex)
	{
		if (menu == MainMenuScript.MainMenu && menuItem == MainMenuScript.MainMenuItems["phone"])
		{
			OnPhoneOpened();
		}
		else if (menu == phoneMenu)
		{
			if (IsSelected("dm"))
			{
				IOrderedEnumerable<PlayerState> players = LatentPlayers.All.OrderBy((PlayerState p) => PartyScript.PartyMembers.Contains(p.Id));
				PlayerListMenu.ShowMenu(phoneMenu, players, async delegate(Menu menu2, int playerId)
				{
					PlayerState playerState = LatentPlayers.Get(playerId);
					string text = await Utils.GetUserInput(LocalizationController.S(Entries.Imenu.IMENU_PHONE_SEND_DM), LocalizationController.S(Entries.Imenu.IMENU_PHONE_SEND_DM_INPUT_TEXT, playerState.NameAndId), "...", 256);
					if (string.IsNullOrWhiteSpace(text))
					{
						Utils.PlayErrorSound();
					}
					else
					{
						API.ExecuteCommand($"dm {playerId} {text}");
					}
				}, delegate(MenuItem menuItem2, int playerId)
				{
					menuItem2.RightIcon = (PartyScript.PartyMembers.Contains(playerId) ? MenuItem.Icon.INV_LINK : MenuItem.Icon.NONE);
				}, exceptMe: true);
			}
			else if (IsSelected("camera"))
			{
				MenuController.CloseAllMenus();
				PhoneCameraScript.OpenCamera();
			}
		}
		else
		{
			if (menu != serviceMenu)
			{
				return;
			}
			MenuController.CloseAllMenus();
			if (IsSelected("police"))
			{
				int num = 0;
				Business closestBusiness = BusinessScript.ClosestBusiness;
				BusinessEmployee robberyEmployee = BusinessScript.RobberyEmployee;
				if (closestBusiness != null && robberyEmployee != null && robberyEmployee.State.IsBeingRobbed)
				{
					num = 1;
					BaseScript.TriggerServerEvent("gtacnr:businesses:robbery:snitch", new object[1] { closestBusiness.Id });
				}
				int currentThief = PickpocketScript.GetCurrentThief();
				if (currentThief != 0)
				{
					PickpocketScript.RegisterPoliceCalled();
					num = 2;
					BaseScript.TriggerServerEvent("gtacnr:crimes:pickpocket:snitch", new object[1] { currentThief });
				}
				BaseScript.TriggerServerEvent("gtacnr:services:requestService", new object[2] { "police", num });
				CallAnim();
			}
			else if (IsSelected("ems"))
			{
				BaseScript.TriggerServerEvent("gtacnr:services:requestService", new object[2] { "ems", 0 });
				CallAnim();
			}
			else if (IsSelected("drugs"))
			{
				BaseScript.TriggerServerEvent("gtacnr:services:requestService", new object[2] { "drugs", 0 });
				CallAnim();
			}
			else if (IsSelected("mech"))
			{
				BaseScript.TriggerServerEvent("gtacnr:services:requestService", new object[2] { "mechanic", 0 });
				CallAnim();
			}
			else if (IsSelected("hitman"))
			{
				HitmanContractMenuScript.ShowMenu(menu);
			}
			else if (IsSelected("weapons"))
			{
				BaseScript.TriggerServerEvent("gtacnr:services:requestService", new object[2] { "weapons", 0 });
				CallAnim();
			}
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

	private async void RefreshPhone()
	{
		phoneMenu.MenuSubtitle = "iFruit 4";
		string phoneItemId = Utils.GetPreference<string>("gtacnr:phoneItem");
		if (phoneItemId == null)
		{
			return;
		}
		InventoryItem itemInfo = Gtacnr.Data.Items.GetItemDefinition(phoneItemId);
		if (itemInfo != null)
		{
			IEnumerable<InventoryEntry> enumerable = InventoryMenuScript.Cache;
			if (enumerable == null)
			{
				enumerable = await InventoryMenuScript.ReloadInventory();
			}
			if (enumerable.Any((InventoryEntry entry) => entry.ItemId == phoneItemId))
			{
				phoneMenu.MenuSubtitle = itemInfo.Name;
			}
		}
	}

	private async void RefreshServices()
	{
		bool flag = Gtacnr.Client.API.Jobs.CachedJobEnum.IsPublicService();
		string text = "~n~" + LocalizationController.S(Entries.Main.NOT_IMPLEMENTED);
		string text2 = (flag ? ("~n~~r~" + LocalizationController.S(Entries.Imenu.IMENU_SERVICES_ERR_PUBLIC)) : "");
		serviceMenu.ClearMenuItems();
		Menu menu = serviceMenu;
		MenuItem item = (menuItems["police"] = new MenuItem(LocalizationController.S(Entries.Imenu.IMENU_SERVICES_POLICE_TEXT), LocalizationController.S(Entries.Imenu.IMENU_SERVICES_POLICE_DESCRIPTION))
		{
			LeftIcon = MenuItem.Icon.GTACNR_POLICE
		});
		menu.AddMenuItem(item);
		Menu menu2 = serviceMenu;
		item = (menuItems["ems"] = new MenuItem(LocalizationController.S(Entries.Imenu.IMENU_SERVICES_EMS_TEXT), LocalizationController.S(Entries.Imenu.IMENU_SERVICES_EMS_DESCRIPTION))
		{
			LeftIcon = MenuItem.Icon.GTACNR_EMS
		});
		menu2.AddMenuItem(item);
		Menu menu3 = serviceMenu;
		item = (menuItems["fire"] = new MenuItem(LocalizationController.S(Entries.Imenu.IMENU_SERVICES_FIRE_TEXT), LocalizationController.S(Entries.Imenu.IMENU_SERVICES_FIRE_DESCRIPTION) + text)
		{
			Enabled = false,
			LeftIcon = MenuItem.Icon.GTACNR_FIREMEN
		});
		menu3.AddMenuItem(item);
		Menu menu4 = serviceMenu;
		item = (menuItems["taxi"] = new MenuItem(LocalizationController.S(Entries.Imenu.IMENU_SERVICES_TAXI_TEXT), LocalizationController.S(Entries.Imenu.IMENU_SERVICES_TAXI_DESCRIPTION) + text)
		{
			Enabled = false,
			LeftIcon = MenuItem.Icon.GTACNR_TAXI
		});
		menu4.AddMenuItem(item);
		Menu menu5 = serviceMenu;
		item = (menuItems["mech"] = new MenuItem(LocalizationController.S(Entries.Imenu.IMENU_SERVICES_MECH_TEXT), LocalizationController.S(Entries.Imenu.IMENU_SERVICES_MECH_DESCRIPTION))
		{
			LeftIcon = MenuItem.Icon.GTACNR_MECHANIC
		});
		menu5.AddMenuItem(item);
		Menu menu6 = serviceMenu;
		item = (menuItems["drugs"] = new MenuItem(LocalizationController.S(Entries.Imenu.IMENU_SERVICES_DRUGS_TEXT), LocalizationController.S(Entries.Imenu.IMENU_SERVICES_DRUGS_DESCRIPTION) + text2)
		{
			Enabled = !flag,
			LeftIcon = MenuItem.Icon.GTACNR_DRUGS
		});
		menu6.AddMenuItem(item);
		Menu menu7 = serviceMenu;
		item = (menuItems["weapons"] = new MenuItem(LocalizationController.S(Entries.Imenu.IMENU_SERVICES_WEAPONS_TEXT), LocalizationController.S(Entries.Imenu.IMENU_SERVICES_WEAPONS_DESCR) + text2)
		{
			Enabled = !flag,
			LeftIcon = MenuItem.Icon.GTACNR_AMMO
		});
		menu7.AddMenuItem(item);
		Menu menu8 = serviceMenu;
		item = (menuItems["hitman"] = new MenuItem(LocalizationController.S(Entries.Imenu.IMENU_SERVICES_HITMAN_TEXT), LocalizationController.S(Entries.Imenu.IMENU_SERVICES_HITMAN_DESCRIPTION) + text2)
		{
			Enabled = !flag,
			LeftIcon = MenuItem.Icon.GTACNR_HITMAN
		});
		menu8.AddMenuItem(item);
	}

	private bool IsPhoneMenuOpen()
	{
		if (!phoneMenu.Visible && !phoneMenu.ChildrenMenus.Any((Menu m) => m.Visible))
		{
			return phoneMenu.ChildrenMenus.SelectMany((Menu m) => m.ChildrenMenus).Any((Menu m) => m.Visible);
		}
		return true;
	}
}
