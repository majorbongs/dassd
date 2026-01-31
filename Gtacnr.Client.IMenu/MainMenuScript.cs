using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Gtacnr.Client.API;
using Gtacnr.Client.API.Scripts;
using Gtacnr.Client.API.UI.Menus;
using Gtacnr.Client.Businesses.Dealerships;
using Gtacnr.Client.Characters.Lifecycle;
using Gtacnr.Client.Jobs.ArmsDealer;
using Gtacnr.Client.Jobs.DrugDealer;
using Gtacnr.Client.Jobs.Hitman;
using Gtacnr.Client.Jobs.Mechanic;
using Gtacnr.Client.Jobs.Paramedic;
using Gtacnr.Client.Jobs.Police;
using Gtacnr.Client.Jobs.PrivateMedic;
using Gtacnr.Client.Jobs.Trucker;
using Gtacnr.Client.Libs;
using Gtacnr.Data;
using Gtacnr.Localization;
using Gtacnr.Model;
using MenuAPI;

namespace Gtacnr.Client.IMenu;

public class MainMenuScript : Script
{
	public static readonly Control OpenMenuControl = (Control)244;

	public static readonly string OpenMenuControlString = "~INPUT_INTERACTION_MENU~";

	private KeyEventType controllerEventType = KeyEventType.Held;

	public static Menu MainMenu { get; private set; }

	public static Dictionary<string, MenuItem> MainMenuItems { get; } = new Dictionary<string, MenuItem>();

	public static Menu StatsAndTasksMenu { get; private set; } = new Menu(LocalizationController.S(Entries.Imenu.IMENU_STATS_AND_TASKS), "");

	protected override async void OnStarted()
	{
		while (!MainScript.LocalizationLoaded)
		{
			await BaseScript.Delay(0);
		}
		MenuController.MenuAlignment = ((!Preferences.MenusLeftAligned.Get()) ? MenuController.MenuAlignmentOption.Right : MenuController.MenuAlignmentOption.Left);
		KeysScript.AttachListener(OpenMenuControl, OnKeyEvent, 10000);
		controllerEventType = (Preferences.MenuDoublePressOnController.Get() ? KeyEventType.DoublePressed : KeyEventType.Held);
		CreateMenus();
		API.SetAudioFlag("LoadMPData", true);
		API.SetAudioFlag("DisableFlightMusic", true);
		Chat.AddSuggestion("/menu", LocalizationController.S(Entries.Imenu.SUGG_MENU));
		API.SetHudColour(205, 0, 124, 234, 255);
		API.SetHudColour(206, 255, 32, 0, 255);
		Gtacnr.Client.API.Jobs.JobChangedEvent += OnJobChangedEvent;
	}

	private void CreateMenus()
	{
		MainMenu = new Menu("~HUD_COLOUR_G14~Cops ~s~and ~HUD_COLOUR_G15~Robbers", "");
		MenuController.AddMenu(MainMenu);
		MainMenu.HeaderFont = Font.Pricedown;
		MainMenu.HeaderFontSize = 900f;
		Menu mainMenu = MainMenu;
		Dictionary<string, MenuItem> mainMenuItems = MainMenuItems;
		MenuItem obj = new MenuItem(LocalizationController.S(Entries.Imenu.IMENU_INVENTORY), LocalizationController.S(Entries.Imenu.IMENU_INVENTORY_DESCR))
		{
			LeftIcon = MenuItem.Icon.GTACNR_INVENTORY
		};
		MenuItem item = obj;
		mainMenuItems["inventory"] = obj;
		mainMenu.AddMenuItem(item);
		Menu mainMenu2 = MainMenu;
		Dictionary<string, MenuItem> mainMenuItems2 = MainMenuItems;
		MenuItem obj2 = new MenuItem(LocalizationController.S(Entries.Imenu.IMENU_WARDROBE), LocalizationController.S(Entries.Imenu.IMENU_WARDROBE_DESCR))
		{
			LeftIcon = MenuItem.Icon.GTACNR_WARDROBE
		};
		item = obj2;
		mainMenuItems2["wardrobe"] = obj2;
		mainMenu2.AddMenuItem(item);
		Menu mainMenu3 = MainMenu;
		Dictionary<string, MenuItem> mainMenuItems3 = MainMenuItems;
		MenuItem obj3 = new MenuItem(LocalizationController.S(Entries.Imenu.IMENU_ARMORY), LocalizationController.S(Entries.Imenu.IMENU_ARMORY_DESCR))
		{
			LeftIcon = MenuItem.Icon.GTACNR_ARMORY
		};
		item = obj3;
		mainMenuItems3["armory"] = obj3;
		mainMenu3.AddMenuItem(item);
		MainMenuItems["job"] = new MenuItem(LocalizationController.S(Entries.Imenu.IMENU_JOB), LocalizationController.S(Entries.Imenu.IMENU_JOB_DESCR_NONE))
		{
			LeftIcon = MenuItem.Icon.GTACNR_JOB,
			Enabled = false
		};
		MainMenu.AddMenuItem(MainMenuItems["job"]);
		Menu mainMenu4 = MainMenu;
		Dictionary<string, MenuItem> mainMenuItems4 = MainMenuItems;
		MenuItem obj4 = new MenuItem(LocalizationController.S(Entries.Imenu.IMENU_PHONE), LocalizationController.S(Entries.Imenu.IMENU_PHONE_DESCR))
		{
			LeftIcon = MenuItem.Icon.GTACNR_SERVICES
		};
		item = obj4;
		mainMenuItems4["phone"] = obj4;
		mainMenu4.AddMenuItem(item);
		Menu mainMenu5 = MainMenu;
		Dictionary<string, MenuItem> mainMenuItems5 = MainMenuItems;
		MenuItem obj5 = new MenuItem(LocalizationController.S(Entries.Imenu.IMENU_VEHICLES), LocalizationController.S(Entries.Imenu.IMENU_VEHICLES_DESCR))
		{
			LeftIcon = MenuItem.Icon.GTACNR_VEHICLES
		};
		item = obj5;
		mainMenuItems5["vehicles"] = obj5;
		mainMenu5.AddMenuItem(item);
		Menu mainMenu6 = MainMenu;
		Dictionary<string, MenuItem> mainMenuItems6 = MainMenuItems;
		MenuItem obj6 = new MenuItem(LocalizationController.S(Entries.Imenu.IMENU_PROPERTIES), LocalizationController.S(Entries.Imenu.IMENU_PROPERTIES_DESCR))
		{
			LeftIcon = MenuItem.Icon.GTACNR_PROPERTIES
		};
		item = obj6;
		mainMenuItems6["properties"] = obj6;
		mainMenu6.AddMenuItem(item);
		Menu mainMenu7 = MainMenu;
		Dictionary<string, MenuItem> mainMenuItems7 = MainMenuItems;
		MenuItem obj7 = new MenuItem(LocalizationController.S(Entries.Imenu.IMENU_STATS_AND_TASKS), LocalizationController.S(Entries.Imenu.IMENU_STATS_AND_TASKS_DESCR))
		{
			LeftIcon = MenuItem.Icon.GTACNR_STATS
		};
		item = obj7;
		mainMenuItems7["statsAndTasks"] = obj7;
		mainMenu7.AddMenuItem(item);
		MenuController.AddSubmenu(MainMenu, StatsAndTasksMenu);
		MenuController.BindMenuItem(MainMenu, StatsAndTasksMenu, MainMenuItems["statsAndTasks"]);
		Menu statsAndTasksMenu = StatsAndTasksMenu;
		Dictionary<string, MenuItem> mainMenuItems8 = MainMenuItems;
		MenuItem obj8 = new MenuItem(LocalizationController.S(Entries.Imenu.IMENU_STATS), LocalizationController.S(Entries.Imenu.IMENU_STATS_DESCR))
		{
			LeftIcon = MenuItem.Icon.GTACNR_STATS
		};
		item = obj8;
		mainMenuItems8["stats"] = obj8;
		statsAndTasksMenu.AddMenuItem(item);
		Menu statsAndTasksMenu2 = StatsAndTasksMenu;
		Dictionary<string, MenuItem> mainMenuItems9 = MainMenuItems;
		MenuItem obj9 = new MenuItem(LocalizationController.S(Entries.Imenu.IMENU_DAILY_CHALLENGES), LocalizationController.S(Entries.Imenu.IMENU_DAILY_CHALLENGES_DESCR))
		{
			LeftIcon = MenuItem.Icon.TICK
		};
		item = obj9;
		mainMenuItems9["dailyChallenges"] = obj9;
		statsAndTasksMenu2.AddMenuItem(item);
		Menu statsAndTasksMenu3 = StatsAndTasksMenu;
		Dictionary<string, MenuItem> mainMenuItems10 = MainMenuItems;
		MenuItem obj10 = new MenuItem(LocalizationController.S(Entries.Imenu.IMENU_ACHIEVEMENTS), LocalizationController.S(Entries.Imenu.IMENU_ACHIEVEMENTS_DESCR))
		{
			LeftIcon = MenuItem.Icon.TROPHY
		};
		item = obj10;
		mainMenuItems10["achievements"] = obj10;
		statsAndTasksMenu3.AddMenuItem(item);
		Menu mainMenu8 = MainMenu;
		Dictionary<string, MenuItem> mainMenuItems11 = MainMenuItems;
		MenuItem obj11 = new MenuItem(LocalizationController.S(Entries.Imenu.IMENU_OPTIONS), LocalizationController.S(Entries.Imenu.IMENU_OPTIONS_DESCR))
		{
			LeftIcon = MenuItem.Icon.GTACNR_OPTIONS
		};
		item = obj11;
		mainMenuItems11["options"] = obj11;
		mainMenu8.AddMenuItem(item);
		Menu mainMenu9 = MainMenu;
		Dictionary<string, MenuItem> mainMenuItems12 = MainMenuItems;
		MenuItem obj12 = new MenuItem(LocalizationController.S(Entries.Imenu.IMENU_HELP), LocalizationController.S(Entries.Imenu.IMENU_HELP_DESCR))
		{
			LeftIcon = MenuItem.Icon.GTACNR_HELP
		};
		item = obj12;
		mainMenuItems12["help"] = obj12;
		mainMenu9.AddMenuItem(item);
	}

	private async void OnJobChangedEvent(object sender, JobArgs e)
	{
		try
		{
			await Refresh(e.CurrentJobId);
		}
		catch (Exception exception)
		{
			Print(exception);
		}
	}

	private async Task Refresh(string jobId = null)
	{
		string id = Game.Player.ServerId.ToString();
		string name = await Authentication.GetAccountName();
		int num = await Users.GetXP();
		int levelByXP = Gtacnr.Utils.GetLevelByXP(num);
		Job jobData = Gtacnr.Data.Jobs.GetJobData(jobId);
		MainMenu.MenuSubtitle = name + " (" + id + ")";
		MainMenu.CounterPreText = $"LVL {levelByXP} - {num} XP";
		if (string.IsNullOrWhiteSpace(jobId) || jobData == null)
		{
			return;
		}
		switch (jobId)
		{
		case "none":
			MainMenuItems["job"].Description = LocalizationController.S(Entries.Imenu.IMENU_JOB_DESCR_NONE);
			MainMenuItems["job"].Enabled = false;
			break;
		case "police":
			while (PoliceMenuScript.Menu == null)
			{
				await BaseScript.Delay(0);
			}
			MainMenuItems["job"].Description = LocalizationController.S(Entries.Imenu.IMENU_JOB_DESCR_POLICE);
			MainMenuItems["job"].Enabled = true;
			MenuController.AddSubmenu(MainMenu, PoliceMenuScript.Menu);
			MenuController.BindMenuItem(MainMenu, PoliceMenuScript.Menu, MainMenuItems["job"]);
			break;
		case "paramedic":
			while (ParamedicMenuScript.Menu == null)
			{
				await BaseScript.Delay(0);
			}
			MainMenuItems["job"].Description = LocalizationController.S(Entries.Imenu.IMENU_JOB_DESCR_PARAMEDIC);
			MainMenuItems["job"].Enabled = true;
			MenuController.AddSubmenu(MainMenu, ParamedicMenuScript.Menu);
			MenuController.BindMenuItem(MainMenu, ParamedicMenuScript.Menu, MainMenuItems["job"]);
			break;
		case "drugDealer":
			while (DrugDealerMenuScript.Menu == null)
			{
				await BaseScript.Delay(0);
			}
			MainMenuItems["job"].Description = LocalizationController.S(Entries.Imenu.IMENU_JOB_DESCR_DRUG_DEALER);
			MainMenuItems["job"].Enabled = true;
			MenuController.AddSubmenu(MainMenu, DrugDealerMenuScript.Menu);
			MenuController.BindMenuItem(MainMenu, DrugDealerMenuScript.Menu, MainMenuItems["job"]);
			break;
		case "mechanic":
			while (MechanicMenuScript.Menu == null)
			{
				await BaseScript.Delay(0);
			}
			MainMenuItems["job"].Description = LocalizationController.S(Entries.Imenu.IMENU_JOB_DESCR_MECHANIC);
			MainMenuItems["job"].Enabled = true;
			MenuController.AddSubmenu(MainMenu, MechanicMenuScript.Menu);
			MenuController.BindMenuItem(MainMenu, MechanicMenuScript.Menu, MainMenuItems["job"]);
			break;
		case "deliveryDriver":
			while (TruckerMenuScript.Menu == null)
			{
				await BaseScript.Delay(0);
			}
			MainMenuItems["job"].Description = LocalizationController.S(Entries.Imenu.IMENU_JOB_DESCR_TRUCKER);
			MainMenuItems["job"].Enabled = true;
			MenuController.AddSubmenu(MainMenu, TruckerMenuScript.Menu);
			MenuController.BindMenuItem(MainMenu, TruckerMenuScript.Menu, MainMenuItems["job"]);
			break;
		case "hitman":
			while (HitmanMenuScript.Menu == null)
			{
				await BaseScript.Delay(0);
			}
			MainMenuItems["job"].Description = HitmanMenuScript.MainMenuItemDescription;
			MainMenuItems["job"].Enabled = true;
			MenuController.AddSubmenu(MainMenu, HitmanMenuScript.Menu);
			MenuController.BindMenuItem(MainMenu, HitmanMenuScript.Menu, MainMenuItems["job"]);
			break;
		case "privateMedic":
			while (PrivateMedicMenuScript.Menu == null)
			{
				await BaseScript.Delay(0);
			}
			MainMenuItems["job"].Description = LocalizationController.S(Entries.Imenu.IMENU_JOB_DESCR_PRIVATE_MEDIC);
			MainMenuItems["job"].Enabled = true;
			MenuController.AddSubmenu(MainMenu, PrivateMedicMenuScript.Menu);
			MenuController.BindMenuItem(MainMenu, PrivateMedicMenuScript.Menu, MainMenuItems["job"]);
			break;
		case "armsDealer":
			while (ArmsDealerMenuScript.Menu == null)
			{
				await BaseScript.Delay(0);
			}
			MainMenuItems["job"].Description = LocalizationController.S(Entries.Imenu.IMENU_JOB_DESCR_ARMS_DEALER);
			MainMenuItems["job"].Enabled = true;
			MenuController.AddSubmenu(MainMenu, ArmsDealerMenuScript.Menu);
			MenuController.BindMenuItem(MainMenu, ArmsDealerMenuScript.Menu, MainMenuItems["job"]);
			break;
		default:
			MainMenuItems["job"].Description = LocalizationController.S(Entries.Imenu.IMENU_JOB_DESCR_UNDEFINED);
			MainMenuItems["job"].Enabled = false;
			break;
		}
	}

	[Command("menu")]
	private void MenuCommand()
	{
		ToggleMenu(true);
	}

	[Command("menudoublepress")]
	private void MenuDoublePressCommand()
	{
		bool flag = !Preferences.MenuDoublePressOnController.Get();
		Preferences.MenuDoublePressOnController.Set(flag);
		Chat.AddMessage($"Menu double press: {flag}");
		controllerEventType = (flag ? KeyEventType.DoublePressed : KeyEventType.Held);
	}

	private async void ToggleMenu(bool? toggle = null)
	{
		if (!DealershipScript.IsInDealership && SpawnScript.HasSpawned)
		{
			bool visible = MainMenu.Visible;
			if (!MainMenu.Visible)
			{
				Menus.CloseAll();
			}
			if (!toggle.HasValue)
			{
				MainMenu.Visible = !visible;
			}
			else
			{
				MainMenu.Visible = toggle.Value;
			}
			await Refresh();
		}
	}

	private bool OnKeyEvent(Control control, KeyEventType eventType, InputType inputType)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		if (control != OpenMenuControl)
		{
			return false;
		}
		if (eventType == KeyEventType.JustPressed && inputType == InputType.Keyboard)
		{
			ToggleMenu();
			return true;
		}
		if (eventType == controllerEventType && inputType == InputType.Controller)
		{
			ToggleMenu(true);
			return true;
		}
		return false;
	}
}
