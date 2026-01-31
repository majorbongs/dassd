using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CitizenFX.Core;
using Gtacnr.Client.API;
using Gtacnr.Client.Businesses.Dealerships;
using Gtacnr.Client.Crimes;
using Gtacnr.Client.Jobs.Police;
using Gtacnr.Data;
using Gtacnr.Localization;
using Gtacnr.Model;
using Gtacnr.Model.Enums;
using MenuAPI;

namespace Gtacnr.Client.Businesses.PoliceStations;

public class PoliceStationMenuScript : Script
{
	private static Dictionary<string, Menu> subMenus = new Dictionary<string, Menu>();

	private static Dictionary<string, MenuItem> menuItems = new Dictionary<string, MenuItem>();

	private static bool isBusy;

	private bool isRefreshing;

	protected override void OnStarted()
	{
		subMenus["listWarrants"] = new Menu(LocalizationController.S(Entries.Businesses.MENU_POLICEFD_STATION_TITLE), LocalizationController.S(Entries.Businesses.MENU_POLICEFD_STATION_ARREST_SUBTITLE))
		{
			MaxDistance = 7.5f
		};
		subMenus["listMostWanted"] = new Menu(LocalizationController.S(Entries.Businesses.MENU_POLICEFD_STATION_TITLE), LocalizationController.S(Entries.Businesses.MENU_POLICEFD_STATION_MW_SUBTITLE))
		{
			MaxDistance = 7.5f
		};
		subMenus["listBounties"] = new Menu(LocalizationController.S(Entries.Businesses.MENU_POLICEFD_STATION_TITLE), LocalizationController.S(Entries.Businesses.MENU_POLICEFD_STATION_BOUNTIES_SUBTITLE))
		{
			MaxDistance = 7.5f
		};
		ShoppingScript.AddExternalItemSelectHandler(OnMenuItemSelect);
		Gtacnr.Client.API.Jobs.JobChangedEvent += OnJobChangedEvent;
		Users.XPChanged += OnXPChanged;
	}

	private void OnJobChangedEvent(object sender, JobArgs e)
	{
		RefreshMenu();
	}

	private void OnXPChanged(object sender, Users.XPEventArgs e)
	{
		RefreshMenu();
	}

	[EventHandler("gtacnr:crimes:wantedLevelChanged")]
	private void OnWantedLevelChanged(int oldLevel, int newLevel)
	{
		RefreshMenu();
	}

	private async void RefreshMenu()
	{
		if (isRefreshing)
		{
			return;
		}
		try
		{
			isRefreshing = true;
			foreach (string item in new string[9] { "listWarrants", "listMostWanted", "listBounties", "vehicles", "payFine", "turnIn", "takeExam", "toCivilian", "toCop" }.Where((string k) => menuItems.ContainsKey(k)))
			{
				menuItems.Remove(item);
			}
			ShoppingScript.ClearExternalMenuItems(BusinessType.PoliceStation);
			PoliceStation station;
			do
			{
				station = PoliceStationsScript.CurrentStation;
				await BaseScript.Delay(0);
			}
			while (station == null);
			while (!Gtacnr.Client.API.Crime.CachedWantedLevel.HasValue || Gtacnr.Client.API.Jobs.CachedJobEnum == JobsEnum.Invalid)
			{
				await BaseScript.Delay(10);
			}
			JobsEnum cachedJobEnum = Gtacnr.Client.API.Jobs.CachedJobEnum;
			int? wl = Gtacnr.Client.API.Crime.CachedWantedLevel;
			if (cachedJobEnum.IsPolice())
			{
				if (station.Dealership != null && DealershipScript.GetDealershipById(station.Dealership) != null)
				{
					menuItems["vehicles"] = new MenuItem("~b~Police Vehicles", "Purchase ~y~vehicles ~s~for the ~b~police ~s~job.")
					{
						RightIcon = MenuItem.Icon.GTACNR_VEHICLES
					};
					ShoppingScript.AddExternalMenuItem(BusinessType.PoliceStation, menuItems["vehicles"], before: true);
				}
				menuItems["listWarrants"] = new MenuItem("Arrest warrants", "Obtain a list of the players that have an outstanding ~o~arrest warrant~s~.")
				{
					Label = Utils.MENU_ARROW
				};
				menuItems["listMostWanted"] = new MenuItem("Most wanted", "Obtain a list of the ~r~most wanted ~s~players.")
				{
					Label = Utils.MENU_ARROW
				};
				menuItems["toCivilian"] = Gtacnr.Data.Jobs.GetJobData("none").ToSwitchMenuItem();
				ShoppingScript.AddExternalMenuItem(BusinessType.PoliceStation, menuItems["listWarrants"], before: true);
				ShoppingScript.AddExternalMenuItem(BusinessType.PoliceStation, menuItems["listMostWanted"], before: true);
				ShoppingScript.AddExternalMenuItem(BusinessType.PoliceStation, menuItems["toCivilian"]);
				ShoppingScript.BindExternalMenuItem(subMenus["listWarrants"], menuItems["listWarrants"]);
				ShoppingScript.BindExternalMenuItem(subMenus["listMostWanted"], menuItems["listMostWanted"]);
				return;
			}
			int num = Gtacnr.Client.API.Crime.CachedFine ?? (await Gtacnr.Client.API.Crime.GetFine());
			int num2 = num;
			bool flag = wl > 1;
			if (wl == 1 && num2 > 0)
			{
				menuItems["payFine"] = new MenuItem("Pay your ~y~tickets", "Pay your outstanding ~y~fine ~s~and lose your ~y~one star~s~.")
				{
					Label = "~y~" + num2.ToCurrencyString()
				};
				ShoppingScript.AddExternalMenuItem(BusinessType.PoliceStation, menuItems["payFine"], before: true);
			}
			else if (flag)
			{
				menuItems["turnIn"] = new MenuItem("Turn ~o~yourself ~s~in", "Turn yourself in, pay your ~o~bail ~s~and lose your ~o~wanted level~s~.");
				ShoppingScript.AddExternalMenuItem(BusinessType.PoliceStation, menuItems["turnIn"], before: true);
			}
			menuItems["listBounties"] = new MenuItem("List ~r~bounties", "Obtain a list of the players that have a ~r~bounty~s~.");
			menuItems["takeExam"] = new MenuItem("Take exam", "Take the ~b~police exam~s~. This is a requirement to become an officer.")
			{
				Enabled = !flag
			};
			menuItems["toCop"] = Gtacnr.Data.Jobs.GetJobData("police").ToSwitchMenuItem();
			ShoppingScript.AddExternalMenuItem(BusinessType.PoliceStation, menuItems["listBounties"], before: true);
			ShoppingScript.AddExternalMenuItem(BusinessType.PoliceStation, menuItems["takeExam"], before: true);
			ShoppingScript.AddExternalMenuItem(BusinessType.PoliceStation, menuItems["toCop"]);
			ShoppingScript.BindExternalMenuItem(subMenus["listBounties"], menuItems["listBounties"]);
		}
		catch (Exception exception)
		{
			Utils.DisplayErrorMessage(4);
			Print(exception);
		}
		finally
		{
			isRefreshing = false;
		}
	}

	private async void RefreshMenuWithPlayersWithWantedLevel(Menu menu1, int minLevel)
	{
		menu1.ClearMenuItems();
		bool flag = false;
		foreach (PlayerState item in LatentPlayers.All)
		{
			if (item.WantedLevel >= minLevel)
			{
				string colorTextCode = item.ColorTextCode;
				string locationName = Utils.GetLocationName(item.Position);
				MenuItem menuItem = new MenuItem(item.ColorNameAndId, $"Wanted Level: {colorTextCode}{item.WantedLevel}\n~s~Location: {colorTextCode}{locationName}");
				_ = Gtacnr.Client.API.Jobs.CachedJob;
				if (minLevel == 5)
				{
					menuItem.Label = "~g~" + item.Bounty.ToCurrencyString();
				}
				else
				{
					menuItem.Label = "~c~" + locationName;
				}
				menu1.AddMenuItem(menuItem);
				flag = true;
			}
		}
		if (!flag)
		{
			menu1.AddMenuItem(new MenuItem("No players :(", "No wanted players found."));
		}
	}

	private async void OnMenuItemSelect(Menu menu, MenuItem menuItem, int itemIndex)
	{
		Vector3 position;
		if (IsSelected("vehicles"))
		{
			PoliceStation currentStation = PoliceStationsScript.CurrentStation;
			if (currentStation.Dealership != null)
			{
				Dealership dealershipById = DealershipScript.GetDealershipById(currentStation.Dealership);
				if (dealershipById == null)
				{
					Utils.PlayErrorSound();
					return;
				}
				MenuController.CloseAllMenus();
				DealershipMenuScript.OpenMenu(dealershipById);
			}
		}
		else if (IsSelected("toCivilian"))
		{
			if (!isBusy)
			{
				await Gtacnr.Client.API.Jobs.TrySwitch("none", "default", null, BeforeSwitching, AfterSwitching);
			}
		}
		else if (IsSelected("toCop"))
		{
			if (!isBusy)
			{
				await Gtacnr.Client.API.Jobs.TrySwitch("police", PoliceStationsScript.CurrentStation.Department, LocalizationController.S(Entries.Jobs.POLICE_SHOOT_WARNING), BeforeSwitching, AfterSwitching);
			}
		}
		else if (IsSelected("takeExam"))
		{
			if (!isBusy)
			{
				menu.CloseMenu();
				ExamScript.StartExam();
			}
		}
		else if (IsSelected("payFine"))
		{
			MenuController.CloseAllMenus();
			if (Gtacnr.Client.API.Crime.CachedWantedLevel != 1)
			{
				Utils.DisplayHelpText("~r~You cannot pay a ticket if you don't have exactly one star.");
				return;
			}
			foreach (Player player in ((BaseScript)this).Players)
			{
				if (player == Game.Player)
				{
					continue;
				}
				PlayerState playerState = LatentPlayers.Get(player);
				if (playerState != null && playerState.JobEnum.IsPolice())
				{
					position = ((Entity)player.Character).Position;
					if (((Vector3)(ref position)).DistanceToSquared(((Entity)Game.PlayerPed).Position) < 2500f)
					{
						Utils.DisplayHelpText("~r~You cannot pay your fine at the police station because there are police officers nearby.");
						return;
					}
				}
			}
			BaseScript.TriggerServerEvent("gtacnr:police:payFineAtPD", new object[0]);
		}
		else if (IsSelected("turnIn"))
		{
			MenuController.CloseAllMenus();
			if (Gtacnr.Client.API.Crime.CachedWantedLevel < 2)
			{
				Utils.DisplayHelpText("~r~You do not have an arrest warrant.");
				return;
			}
			foreach (Player player2 in ((BaseScript)this).Players)
			{
				if (player2 == Game.Player)
				{
					continue;
				}
				PlayerState playerState2 = LatentPlayers.Get(player2);
				if (playerState2 != null && playerState2.JobEnum.IsPolice())
				{
					position = ((Entity)player2.Character).Position;
					if (((Vector3)(ref position)).DistanceToSquared(((Entity)Game.PlayerPed).Position) < 2500f)
					{
						Utils.DisplayHelpText("~r~You cannot turn yourself in at the police station because there are police officers nearby.");
						return;
					}
				}
			}
			PickpocketScript.Instance.CancelWalletTheftMission();
			BaseScript.TriggerServerEvent("gtacnr:police:turnMyselfIn", new object[0]);
		}
		else if (IsSelected("listWarrants"))
		{
			RefreshMenuWithPlayersWithWantedLevel(subMenus["listWarrants"], 2);
		}
		else if (IsSelected("listMostWanted"))
		{
			RefreshMenuWithPlayersWithWantedLevel(subMenus["listMostWanted"], 5);
		}
		else if (IsSelected("listBounties"))
		{
			RefreshMenuWithPlayersWithWantedLevel(subMenus["listBounties"], 5);
		}
		static async Task AfterSwitching()
		{
			await Utils.FadeIn(500);
			MenuController.CloseAllMenus();
			isBusy = false;
		}
		static async Task BeforeSwitching()
		{
			isBusy = true;
			MenuController.CloseAllMenus();
			await Utils.FadeOut(500);
		}
		bool IsSelected(string selection)
		{
			if (menuItems.ContainsKey(selection))
			{
				return menuItem == menuItems[selection];
			}
			return false;
		}
	}
}
