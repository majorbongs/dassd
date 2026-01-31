using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Gtacnr.Client.Anticheat;
using Gtacnr.Client.API;
using Gtacnr.Client.Businesses.Dealerships;
using Gtacnr.Client.Events.Holidays.AprilsFools;
using Gtacnr.Client.Jobs.Police.Arrest;
using Gtacnr.Client.Jobs.Trucker;
using Gtacnr.Client.Premium;
using Gtacnr.Client.Vehicles;
using Gtacnr.Data;
using Gtacnr.Localization;
using Gtacnr.Model;
using Gtacnr.Model.Enums;
using Gtacnr.ResponseCodes;
using MenuAPI;
using Rock.Collections;

namespace Gtacnr.Client.IMenu;

public class VehiclesMenuScript : Script
{
	private static VehiclesMenuScript script;

	private static Dictionary<string, MenuItem> menuItems = new Dictionary<string, MenuItem>();

	private static bool isBusy;

	private static HashSet<StoredVehicle> vehiclesCache;

	private static int sellPriceCache;

	private static StoredVehicle selectedVehicle;

	private static Random random = new Random();

	private static VehicleServiceInfo serviceInfo = Gtacnr.Utils.LoadJson<VehicleServiceInfo>("data/vehicles/vehicleServices.json");

	private static Vector4 valetSpawnCoords;

	private static Ped valet;

	private static DateTime lastRefreshT;

	private static readonly OrderedHashSet<string> favoriteVehicles = Preferences.FavoriteVehicles.Get();

	public static Vehicle CurrentSummonVehicle;

	private static StoredVehicle currentSummonStoredVehicle;

	private static Dictionary<string, List<string>> extraVehicles = new Dictionary<string, List<string>>();

	public static Menu CarsMenu { get; private set; }

	public static Menu ManageMenu { get; private set; }

	public static Menu HistoryMenu { get; private set; }

	public static HashSet<StoredVehicle> VehicleCache => vehiclesCache;

	public VehiclesMenuScript()
	{
		script = this;
		Gtacnr.Client.API.Jobs.JobChangedEvent += OnbChangedEvent;
	}

	protected override async void OnStarted()
	{
		while (MainMenuScript.MainMenu == null)
		{
			await BaseScript.Delay(0);
		}
		CarsMenu = new Menu(LocalizationController.S(Entries.Vehicles.MENU_VEHICLES_TITLE), LocalizationController.S(Entries.Vehicles.MENU_VEHICLES_SUBTITLE));
		MenuController.AddSubmenu(MainMenuScript.MainMenu, CarsMenu);
		MenuController.BindMenuItem(MainMenuScript.MainMenu, CarsMenu, MainMenuScript.MainMenuItems["vehicles"]);
		MainMenuScript.MainMenu.OnItemSelect += OnMenuItemSelect;
		CarsMenu.InstructionalButtons.Clear();
		CarsMenu.InstructionalButtons.Add((Control)201, LocalizationController.S(Entries.Vehicles.MENU_VEHICLES_MANAGE_BUTTON));
		CarsMenu.InstructionalButtons.Add((Control)202, LocalizationController.S(Entries.Main.BTN_BACK));
		CarsMenu.InstructionalButtons.Add((Control)206, LocalizationController.S(Entries.Main.BTN_SEARCH));
		CarsMenu.InstructionalButtons.Add((Control)204, LocalizationController.S(Entries.Vehicles.MENU_VEHICLES_FAVORITE_BUTTON));
		CarsMenu.InstructionalButtons.Add((Control)327, LocalizationController.S(Entries.Main.BTN_REFRESH));
		CarsMenu.OnItemSelect += OnMenuItemSelect;
		CarsMenu.ButtonPressHandlers.Add(new Menu.ButtonPressHandler((Control)206, Menu.ControlPressCheckType.JUST_PRESSED, async delegate
		{
			await SearchVehicles(CarsMenu);
		}, disableControl: true));
		CarsMenu.ButtonPressHandlers.Add(new Menu.ButtonPressHandler((Control)327, Menu.ControlPressCheckType.JUST_PRESSED, delegate(Menu menu, Control control)
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			RefreshButtonHandler(menu, control, delegate
			{
				RefreshManageMenu();
				RefreshMenu();
			});
		}, disableControl: true));
		CarsMenu.ButtonPressHandlers.Add(new Menu.ButtonPressHandler((Control)204, Menu.ControlPressCheckType.JUST_PRESSED, FavouriteButtonHandler, disableControl: true));
		ManageMenu = new Menu(LocalizationController.S(Entries.Vehicles.MENU_VEHICLES_TITLE), LocalizationController.S(Entries.Vehicles.MENU_VEHICLES_MANAGE_SUBTITLE));
		MenuController.AddSubmenu(CarsMenu, ManageMenu);
		ManageMenu.OnItemSelect += OnMenuItemSelect;
		HistoryMenu = new Menu(LocalizationController.S(Entries.Vehicles.MENU_VEHICLES_TITLE), LocalizationController.S(Entries.Vehicles.MENU_VEHICLES_OWNERSHIPHIST_SUBTITLE));
		MenuController.AddSubmenu(ManageMenu, HistoryMenu);
	}

	public static void RefreshButtonHandler(Menu menu, Control control, Action onSuccess)
	{
		if (!Gtacnr.Utils.CheckTimePassed(lastRefreshT, 5000.0))
		{
			Utils.PlayErrorSound();
			return;
		}
		lastRefreshT = DateTime.UtcNow;
		Utils.PlaySelectSound();
		InvalidateCache();
		onSuccess?.Invoke();
	}

	public static void FavouriteButtonHandler(Menu menu, Control control)
	{
		MenuItem currentMenuItem = menu.GetCurrentMenuItem();
		if (currentMenuItem?.ItemData is StoredVehicle storedVehicle)
		{
			string vehicleFullName = Utils.GetVehicleFullName(storedVehicle.Model);
			if (IsVehicleFavorite(storedVehicle.LicensePlate))
			{
				Utils.SendNotification(LocalizationController.S(Entries.Vehicles.MENU_VEHICLES_FAVORITE_REMOVED, vehicleFullName));
				RemoveFavoriteVehicle(storedVehicle.LicensePlate);
				currentMenuItem.RightIcon = MenuItem.Icon.NONE;
			}
			else
			{
				Utils.SendNotification(LocalizationController.S(Entries.Vehicles.MENU_VEHICLES_FAVORITE_ADDED, vehicleFullName));
				AddFavoriteVehicle(storedVehicle.LicensePlate);
				currentMenuItem.RightIcon = MenuItem.Icon.MISSION_STAR;
			}
			Utils.PlaySelectSound();
		}
	}

	public static async Task SearchVehicles(Menu menu)
	{
		string input = await Utils.GetUserInput(LocalizationController.S(Entries.Main.BTN_SEARCH), LocalizationController.S(Entries.Vehicles.MENU_VEHICLES_SEARCH_TEXT), "", 32);
		if (string.IsNullOrWhiteSpace(input))
		{
			menu.ResetFilter();
		}
		else
		{
			input = input.ToLowerInvariant();
			menu.FilterMenuItems(delegate(MenuItem item)
			{
				if (item.ItemData is StoredVehicle storedVehicle)
				{
					try
					{
						return Utils.GetVehicleFullName(storedVehicle.Model).ToLowerInvariant().Contains(input) || storedVehicle.LicensePlate.ToLowerInvariant().Contains(input);
					}
					catch
					{
						return false;
					}
				}
				return false;
			});
		}
		Utils.PlaySelectSound();
	}

	public static async Task EnsureVehicleCache()
	{
		if (vehiclesCache != null)
		{
			return;
		}
		string text = await script.TriggerServerEventAsync<string>("gtacnr:vehicles:getAll", new object[1] { true });
		if (string.IsNullOrEmpty(text))
		{
			CarsMenu.ClearMenuItems();
			CarsMenu.AddMenuItem(new MenuItem(LocalizationController.S(Entries.Main.MENU_ERROR_ITEM)));
			return;
		}
		vehiclesCache = new HashSet<StoredVehicle>(from sv in text.Unjson<List<StoredVehicle>>()
			orderby IsVehicleFavorite(sv.LicensePlate) descending, sv.GarageId, sv.GarageParkIndex
			select sv);
	}

	public static void DeliverVehicle(string licensePlate)
	{
		script.Deliver(licensePlate);
	}

	private async void Deliver(string licensePlate)
	{
		if (isBusy)
		{
			Utils.PlayErrorSound();
			return;
		}
		if (string.IsNullOrEmpty(licensePlate))
		{
			Utils.PlayErrorSound();
			return;
		}
		if (((Entity)Game.PlayerPed).IsDead)
		{
			Utils.DisplayHelpText(LocalizationController.S(Entries.Vehicles.CANT_DELIVER_WHEN_DEAD));
			return;
		}
		if (Game.PlayerPed.IsCuffed)
		{
			Utils.DisplayHelpText(LocalizationController.S(Entries.Vehicles.CANT_DO_WHEN_CUFFED));
			return;
		}
		if ((Entity)(object)ActiveVehicleScript.ActiveVehicle == (Entity)(object)TruckerJobScript.DeliveryVehicle && (Entity)(object)ActiveVehicleScript.ActiveVehicle != (Entity)null)
		{
			Utils.DisplayHelpText(LocalizationController.S(Entries.Player.CANT_SUMMON_VEHICLE_WHEN_PART_OF_MISSION));
			return;
		}
		try
		{
			isBusy = true;
			await EnsureVehicleCache();
			StoredVehicle selVeh = vehiclesCache.FirstOrDefault((StoredVehicle v) => v.LicensePlate?.ToUpperInvariant() == licensePlate.ToUpperInvariant());
			if (selVeh == null)
			{
				Utils.DisplayHelpText(LocalizationController.S(Entries.Player.DONT_HAVE_VEHICLE_WITH_PLATE));
				return;
			}
			PersonalVehicleModel personalVehicleModel = DealershipScript.FindVehicleModelData(selVeh.Model);
			if (personalVehicleModel != null && personalVehicleModel.WasRecalled)
			{
				string vehicleFullName = Utils.GetVehicleFullName(selVeh.Model);
				vehicleFullName = ((!string.IsNullOrEmpty(vehicleFullName)) ? LocalizationController.S(Entries.Player.VEHICLE_RECALLED_VALID_NAME, vehicleFullName) : LocalizationController.S(Entries.Player.VEHICLE_RECALLED_INVALID_NAME));
				Utils.DisplayHelpText(LocalizationController.S(Entries.Player.VEHICLE_RECALLED, vehicleFullName));
				return;
			}
			MenuController.CloseAllMenus();
			if (!PrepareSummonPosition())
			{
				Utils.DisplayHelpText(LocalizationController.S(Entries.Player.UNABLE_TO_DELIVER_MOVE));
				return;
			}
			Vehicle activeVehicle = ActiveVehicleScript.ActiveVehicle;
			string text = null;
			if (ActiveVehicleScript.ActiveVehicleStoredId == selVeh.Id && (Entity)(object)activeVehicle != (Entity)null)
			{
				text = ActiveVehicleScript.ActiveVehicleHealthData.Json();
				((PoolObject)activeVehicle).Delete();
			}
			if (ResolveSummonVehicleResponse((SummonVehicleResponse)(await TriggerServerEventAsync<int>("gtacnr:vehicles:summon", new object[2] { selVeh.Id, text })), 57))
			{
				string vehicleFullName2 = Utils.GetVehicleFullName(selVeh.Model);
				Utils.DisplayHelpText(LocalizationController.S(Entries.Player.VEHICLE_ON_THE_WAY, vehicleFullName2));
				SummonVehicle(selVeh);
				InvalidateCache();
			}
		}
		finally
		{
			isBusy = false;
		}
	}

	[EventHandler("gtacnr:vehicles:store")]
	private async void Store()
	{
		if (isBusy)
		{
			Utils.PlayErrorSound();
			return;
		}
		try
		{
			isBusy = true;
			Vehicle vehicle = ActiveVehicleScript.ActiveVehicle;
			if ((Entity)(object)vehicle == (Entity)(object)TruckerJobScript.DeliveryVehicle && (Entity)(object)vehicle != (Entity)null)
			{
				Utils.DisplayHelpText(LocalizationController.S(Entries.Player.CANT_STORE_VEHICLE_WHEN_PART_OF_MISSION));
				return;
			}
			if (Game.PlayerPed.IsInVehicle(vehicle))
			{
				Utils.DisplayHelpText(LocalizationController.S(Entries.Player.CANT_STORE_VEHICLE_WHEN_INSIDE));
				return;
			}
			if (((Entity)Game.PlayerPed).IsDead)
			{
				Utils.DisplayHelpText(LocalizationController.S(Entries.Vehicles.CANT_STORE_WHEN_DEAD));
				return;
			}
			if (Game.PlayerPed.IsCuffed)
			{
				Utils.DisplayHelpText(LocalizationController.S(Entries.Vehicles.CANT_STORE_WHEN_CUFFED));
				return;
			}
			MenuController.CloseAllMenus();
			if (ResolveSummonVehicleResponse((SummonVehicleResponse)(await TriggerServerEventAsync<int>("gtacnr:vehicles:storeActive", new object[1] { ActiveVehicleScript.ActiveVehicleHealthData.Json() })), 64))
			{
				string vehicleFullName = Utils.GetVehicleFullName(Model.op_Implicit((vehicle != null) ? ((Entity)vehicle).Model : null)).Trim();
				if (string.IsNullOrEmpty(vehicleFullName))
				{
					vehicleFullName = LocalizationController.S(Entries.Vehicles.VEHICLE_RETURNED_GARAGE_INVALID_VEHICLE);
				}
				if (await ActiveVehicleScript.ResetActiveVehicle(clientOnly: true))
				{
					Utils.DisplayHelpText(LocalizationController.S(Entries.Vehicles.VEHICLE_RETURNED_GARAGE, vehicleFullName));
					InvalidateCache();
				}
			}
		}
		finally
		{
			isBusy = false;
		}
	}

	private async void OnMenuItemSelect(Menu menu, MenuItem menuItem, int itemIndex)
	{
		if (menu == MainMenuScript.MainMenu && menuItem == MainMenuScript.MainMenuItems["vehicles"])
		{
			RefreshMenu();
		}
		else if (menu == CarsMenu && menuItem.ItemData is StoredVehicle storedVehicle)
		{
			selectedVehicle = storedVehicle;
			RefreshManageMenu();
		}
		else
		{
			if (menu != ManageMenu)
			{
				return;
			}
			if (CuffedScript.IsCuffed || CuffedScript.IsInCustody)
			{
				Utils.DisplayHelpText(LocalizationController.S(Entries.Vehicles.CANT_DO_WHEN_CUFFED));
				MenuController.CloseAllMenus();
			}
			else if (IsSelected("summon"))
			{
				Deliver(selectedVehicle.LicensePlate);
			}
			else if (IsSelected("store"))
			{
				Store();
			}
			else if (IsSelected("maintenance"))
			{
				if (isBusy)
				{
					Utils.PlayErrorSound();
					return;
				}
				if (currentSummonStoredVehicle == null || !(selectedVehicle.LicensePlate == currentSummonStoredVehicle.LicensePlate))
				{
					try
					{
						isBusy = true;
						StoredVehicle selVeh = selectedVehicle;
						if ((Entity)(object)ActiveVehicleScript.ActiveVehicle != (Entity)null && selVeh.Id == ActiveVehicleScript.ActiveVehicleStoredId)
						{
							if ((Entity)(object)ActiveVehicleScript.ActiveVehicle == (Entity)(object)TruckerJobScript.DeliveryVehicle && (Entity)(object)ActiveVehicleScript.ActiveVehicle != (Entity)null)
							{
								Utils.DisplayHelpText(LocalizationController.S(Entries.Player.CANT_MAINTENANCE_VEHICLE_WHEN_PART_OF_MISSION));
								return;
							}
							if (Game.PlayerPed.IsInVehicle(ActiveVehicleScript.ActiveVehicle))
							{
								Utils.DisplayHelpText(LocalizationController.S(Entries.Vehicles.CANT_SEND_PERSONAL_VEHICLE_TO_MAINTENANCE_WHEN_INSIDE));
								return;
							}
						}
						string vehicleFullName = Utils.GetVehicleFullName(selVeh.Model);
						SendVehicleToMaintenanceResponse sendVehicleToMaintenanceResponse = (SendVehicleToMaintenanceResponse)(await TriggerServerEventAsync<int>("gtacnr:vehicles:sendToMaintenance", new object[1] { selVeh.Id }));
						switch (sendVehicleToMaintenanceResponse)
						{
						case SendVehicleToMaintenanceResponse.Success:
							Utils.DisplayHelpText(LocalizationController.S(Entries.Vehicles.VEHICLE_SENT_TO_MAINTENANCE, vehicleFullName));
							selVeh.IsInMaintenance = true;
							RefreshManageMenu();
							RefreshMenu();
							if (selVeh.Id == ActiveVehicleScript.ActiveVehicleStoredId)
							{
								await ActiveVehicleScript.ResetActiveVehicle(clientOnly: true);
							}
							break;
						case SendVehicleToMaintenanceResponse.NoMoney:
							Utils.DisplayHelpText(LocalizationController.S(Entries.Main.NOT_ENOUGH_MONEY));
							break;
						case SendVehicleToMaintenanceResponse.AlreadyInMaintenance:
							Utils.DisplayHelpText(LocalizationController.S(Entries.Vehicles.VEHICLE_ALREADY_IN_MAINTENANCE));
							break;
						case SendVehicleToMaintenanceResponse.AttachedToTowtruck:
							Utils.DisplayHelpText(LocalizationController.S(Entries.Player.VEHICLE_ATTACHED_TO_TOW_TRUCK));
							break;
						default:
							Utils.DisplayErrorMessage(103, (int)sendVehicleToMaintenanceResponse);
							break;
						}
						return;
					}
					finally
					{
						isBusy = false;
					}
				}
				Utils.PlayErrorSound();
			}
			else if (IsSelected("replace"))
			{
				if (!isBusy)
				{
					try
					{
						isBusy = true;
						StoredVehicle selVeh = selectedVehicle;
						string vehicleFullName = Utils.GetVehicleFullName(selVeh.Model);
						if (await TriggerServerEventAsync<bool>("gtacnr:vehicles:restoreDeadVehicle", new object[1] { selVeh.Id }))
						{
							if (selVeh.RentData == null)
							{
								Utils.DisplayHelpText(LocalizationController.S(Entries.Vehicles.VEHICLE_REPLACED_TRANSPORTED_GARAGE, vehicleFullName));
							}
							else
							{
								Utils.DisplayHelpText(LocalizationController.S(Entries.Vehicles.VEHICLE_REPLACED, vehicleFullName));
							}
							selVeh.IsDead = false;
							RefreshManageMenu();
							RefreshMenu();
						}
						else
						{
							Utils.DisplayErrorMessage();
						}
						return;
					}
					finally
					{
						isBusy = false;
					}
				}
				Utils.PlayErrorSound();
			}
			else if (IsSelected("rentRenew"))
			{
				if (!isBusy)
				{
					try
					{
						isBusy = true;
						StoredVehicle selVeh = selectedVehicle;
						string vehicleFullName = Utils.GetVehicleModelName(selVeh.Model);
						int renewAmount = selVeh.RentData.RenewPrice;
						if (await Utils.ShowConfirm(LocalizationController.S(Entries.Vehicles.MENU_VEHICLES_RENTAL_EXTEND_CONFIRM_MESSAGE, vehicleFullName, renewAmount.ToCurrencyString()), LocalizationController.S(Entries.Vehicles.MENU_VEHICLES_RENTAL_EXTEND_CONFIRM_TITLE)))
						{
							ExtendRentalResponse extendRentalResponse = (ExtendRentalResponse)(await TriggerServerEventAsync<int>("gtacnr:vehicles:extendRental", new object[1] { selVeh.Id }));
							if (extendRentalResponse == ExtendRentalResponse.Success)
							{
								MenuController.GetCurrentMenu().GoBack();
								Utils.DisplayHelpText(LocalizationController.S(Entries.Vehicles.VEHICLE_RENTAL_EXTENDED_ANOTHER_GAME_WEEK, vehicleFullName, renewAmount.ToCurrencyString()));
								InvalidateCache();
								RefreshManageMenu();
								RefreshMenu();
							}
							else
							{
								Utils.DisplayErrorMessage(130, (int)extendRentalResponse);
							}
						}
						return;
					}
					finally
					{
						isBusy = false;
					}
				}
				Utils.PlayErrorSound();
			}
			else if (IsSelected("rentEnd"))
			{
				if (!isBusy)
				{
					try
					{
						isBusy = true;
						StoredVehicle selVeh = selectedVehicle;
						string vehicleFullName = Utils.GetVehicleModelName(selVeh.Model);
						if (await Utils.ShowConfirm(LocalizationController.S(Entries.Vehicles.MENU_VEHICLES_RENTAL_TERMINATE_CONFIRM_MESSAGE, vehicleFullName), LocalizationController.S(Entries.Vehicles.MENU_VEHICLES_RENTAL_TERMINATE_CONFIRM_TITLE)))
						{
							ExtendRentalResponse extendRentalResponse2 = (ExtendRentalResponse)(await TriggerServerEventAsync<int>("gtacnr:vehicles:terminateRental", new object[1] { selVeh.Id }));
							if (extendRentalResponse2 == ExtendRentalResponse.Success)
							{
								MenuController.GetCurrentMenu().GoBack();
								Utils.DisplayHelpText(LocalizationController.S(Entries.Vehicles.VEHICLE_RENTAL_TERMINATED, vehicleFullName));
								InvalidateCache();
								RefreshManageMenu();
								RefreshMenu();
							}
							else
							{
								Utils.DisplayErrorMessage(131, (int)extendRentalResponse2);
							}
						}
						return;
					}
					finally
					{
						isBusy = false;
					}
				}
				Utils.PlayErrorSound();
			}
			else
			{
				if (!IsSelected("sell"))
				{
					return;
				}
				if (isBusy)
				{
					Utils.PlayErrorSound();
					return;
				}
				if (currentSummonStoredVehicle == null || !(selectedVehicle.LicensePlate == currentSummonStoredVehicle.LicensePlate))
				{
					try
					{
						isBusy = true;
						StoredVehicle selVeh = selectedVehicle;
						string vehicleFullName = Utils.GetVehicleModelName(selVeh.Model);
						if (await Utils.ShowConfirm(LocalizationController.S(Entries.Vehicles.MENU_VEHICLES_SELL_CONFIRM_MESSAGE, vehicleFullName, sellPriceCache.ToCurrencyString()), LocalizationController.S(Entries.Vehicles.MENU_VEHICLES_SELL_CONFIRM_TITLE)))
						{
							MenuController.CloseAllMenus();
							SellVehicleResponse sellVehicleResponse = await TriggerServerEventAsync<SellVehicleResponse>("gtacnr:vehicles:sell", new object[1] { selVeh.Id });
							if (sellVehicleResponse == SellVehicleResponse.Success)
							{
								Utils.DisplayHelpText(LocalizationController.S(Entries.Vehicles.VEHICLE_SOLD_TO_SERVER, vehicleFullName, sellPriceCache.ToCurrencyString()));
								InvalidateCache();
								RefreshManageMenu();
								RefreshMenu();
							}
							else
							{
								Utils.DisplayErrorMessage(93, (int)sellVehicleResponse, sellVehicleResponse.ToString());
							}
						}
						return;
					}
					finally
					{
						isBusy = false;
					}
				}
				Utils.PlayErrorSound();
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

	private void OnbChangedEvent(object sender, JobArgs e)
	{
		InvalidateCache();
	}

	private static bool AddFavoriteVehicle(string licensePlate)
	{
		favoriteVehicles.Add(licensePlate);
		Preferences.FavoriteVehicles.Set(favoriteVehicles);
		return true;
	}

	private static bool RemoveFavoriteVehicle(string licensePlate)
	{
		favoriteVehicles.Remove(licensePlate);
		Preferences.FavoriteVehicles.Set(favoriteVehicles);
		return true;
	}

	public static bool IsVehicleFavorite(string licensePlate)
	{
		return favoriteVehicles.Contains(licensePlate);
	}

	private async void RefreshMenu()
	{
		_ = 2;
		try
		{
			CarsMenu.ClearMenuItems();
			CarsMenu.ResetFilter();
			CarsMenu.AddLoadingMenuItem();
			await EnsureVehicleCache();
			string text = Gtacnr.Client.API.Jobs.CachedJob;
			if (text == null)
			{
				text = await Gtacnr.Client.API.Jobs.GetCurrentJobId();
			}
			Job jobData = Gtacnr.Data.Jobs.GetJobData(text);
			await Money.GetCachedBalanceOrFetch(AccountType.Bank);
			CarsMenu.ClearMenuItems();
			Dictionary<string, MenuItem> dictionary = new Dictionary<string, MenuItem>();
			if (vehiclesCache.Count == 0)
			{
				AddNoVehiclesItem();
				return;
			}
			int num = 0;
			if (vehiclesCache.Select((StoredVehicle v) => v.Id).Contains<string>(ActiveVehicleScript.ActiveVehicleStoredId))
			{
				StoredVehicle storedVehicle = vehiclesCache.FirstOrDefault((StoredVehicle v) => v.Id == ActiveVehicleScript.ActiveVehicleStoredId);
				AddCarMenuItem(storedVehicle, active: true);
				num++;
			}
			foreach (StoredVehicle item in vehiclesCache)
			{
				if (item.Id == ActiveVehicleScript.ActiveVehicleStoredId)
				{
					continue;
				}
				if (jobData.SeparateVehicles)
				{
					if (item.Job != jobData.Id)
					{
						continue;
					}
				}
				else
				{
					if (item.Job != jobData.Id && item.Job != null)
					{
						continue;
					}
					string key;
					string text2;
					if (IsVehicleFavorite(item.LicensePlate))
					{
						key = "*";
						text2 = LocalizationController.S(Entries.Vehicles.MENU_VEHICLES_CATEGORY_FAVORITE);
					}
					else if (item.RentData != null)
					{
						key = "#";
						text2 = LocalizationController.S(Entries.Vehicles.MENU_VEHICLES_CATEGORY_RENTED);
					}
					else
					{
						Garage garageById = Garages.GetGarageById(item.GarageId);
						if (garageById == null)
						{
							Print("Unable to create appropriate category for vehicle: " + item.LicensePlate);
							continue;
						}
						key = garageById.Id;
						text2 = garageById.Name;
					}
					if (!dictionary.ContainsKey(key))
					{
						dictionary[key] = Utils.GetSpacerMenuItem("\u02c5 " + text2 + " \u02c5");
						CarsMenu.AddMenuItem(dictionary[key]);
					}
				}
				AddCarMenuItem(item);
				num++;
			}
			if (num == 0)
			{
				AddNoVehiclesItem();
				return;
			}
			CarsMenu.CounterPreText = LocalizationController.S(Entries.Vehicles.MENU_VEHICLES_COUNTER_TEXT, num);
			void AddNoVehiclesItem()
			{
				if (jobData.SeparateVehicles)
				{
					CarsMenu.AddMenuItem(new MenuItem(LocalizationController.S(Entries.Vehicles.MENU_VEHICLES_EMPTY_JOB_TEXT))
					{
						Description = LocalizationController.S(Entries.Vehicles.MENU_VEHICLES_EMPTY_JOB_DESCRIPTION),
						Enabled = false
					});
				}
				else
				{
					CarsMenu.AddMenuItem(new MenuItem(LocalizationController.S(Entries.Vehicles.MENU_VEHICLES_EMPTY_PERSONAL_TEXT))
					{
						Description = LocalizationController.S(Entries.Vehicles.MENU_VEHICLES_EMPTY_PERSONAL_DESCRIPTION),
						Enabled = false
					});
				}
			}
		}
		catch (Exception ex)
		{
			Print(ex);
			CarsMenu.ClearMenuItems();
			CarsMenu.AddErrorMenuItem(ex);
		}
		void AddCarMenuItem(StoredVehicle storedVehicle2, bool active = false)
		{
			try
			{
				MenuItem menuItem = storedVehicle2.ToMenuItem();
				if (IsVehicleFavorite(storedVehicle2.LicensePlate))
				{
					menuItem.RightIcon = MenuItem.Icon.MISSION_STAR;
				}
				CarsMenu.AddMenuItem(menuItem);
				MenuController.BindMenuItem(CarsMenu, ManageMenu, menuItem);
			}
			catch (Exception ex2)
			{
				Print(ex2);
				MenuItem menuItem2 = CarsMenu.AddErrorMenuItem(ex2);
				if (!string.IsNullOrEmpty(storedVehicle2.LicensePlate))
				{
					menuItem2.Label = storedVehicle2.LicensePlate;
				}
			}
		}
	}

	private async void RefreshManageMenu()
	{
		_ = 1;
		try
		{
			string text = Gtacnr.Client.API.Jobs.CachedJob;
			if (text == null)
			{
				text = await Gtacnr.Client.API.Jobs.GetCurrentJobId();
			}
			Gtacnr.Data.Jobs.GetJobData(text);
			long num = await Money.GetCachedBalanceOrFetch(AccountType.Bank);
			MembershipTier currentMembershipTier = MembershipScript.GetCurrentMembershipTier();
			bool flag = selectedVehicle.RentData != null;
			string vehicleModelName = Utils.GetVehicleModelName(selectedVehicle.Model);
			ManageMenu.MenuSubtitle = (flag ? LocalizationController.S(Entries.Vehicles.MENU_VEHICLES_MANAGE_SUBTITLE_VAR_RENTED, vehicleModelName) : LocalizationController.S(Entries.Vehicles.MENU_VEHICLES_MANAGE_SUBTITLE_VAR, vehicleModelName));
			ManageMenu.ClearMenuItems();
			PersonalVehicleModel personalVehicleModel = DealershipScript.FindVehicleModelData(selectedVehicle.Model);
			DealershipSupply dealershipSupply = DealershipScript.FindFirstSupplyOfModel(selectedVehicle.Model);
			float num2 = 1f;
			if (personalVehicleModel != null && personalVehicleModel.ServiceMultipliers.TryGetValue("Summon", out var value))
			{
				num2 = value;
			}
			int num3 = Convert.ToInt32((dealershipSupply != null) ? ((float)serviceInfo.Summon.CalculatePrice(dealershipSupply.Price) * num2) : ((float)serviceInfo.Summon.DefaultPrice));
			num2 = 1f;
			if (personalVehicleModel != null && personalVehicleModel.ServiceMultipliers.TryGetValue("Maintenance", out value))
			{
				num2 = value;
			}
			int num4 = Convert.ToInt32((dealershipSupply != null) ? ((float)serviceInfo.Maintenance.CalculatePrice(dealershipSupply.Price) * num2) : ((float)serviceInfo.Maintenance.DefaultPrice));
			num2 = 1f;
			if (personalVehicleModel != null && personalVehicleModel.ServiceMultipliers.TryGetValue("Replace", out value))
			{
				num2 = value;
			}
			int num5 = Convert.ToInt32((dealershipSupply != null) ? ((float)serviceInfo.Replace.CalculatePrice(dealershipSupply.Price) * num2) : ((float)serviceInfo.Replace.DefaultPrice));
			string text2 = LocalizationController.S(Entries.Vehicles.MENU_VEHICLES_STORE_YOUR_GARAGE);
			Garage garageById = Garages.GetGarageById(selectedVehicle.GarageId);
			if (garageById != null)
			{
				text2 = garageById.Name;
			}
			bool flag2 = flag && DateTime.UtcNow > selectedVehicle.RentData.EndDateTime;
			bool flag3 = true;
			MenuItem item;
			if (selectedVehicle.Id == ActiveVehicleScript.ActiveVehicleStoredId)
			{
				Menu manageMenu = ManageMenu;
				item = (menuItems["store"] = new MenuItem(LocalizationController.S(Entries.Vehicles.MENU_VEHICLES_STORE_TITLE), flag ? LocalizationController.S(Entries.Vehicles.MENU_VEHICLES_STORE_DESCRIPTION_RENTED, vehicleModelName) : LocalizationController.S(Entries.Vehicles.MENU_VEHICLES_STORE_DESCRIPTION, vehicleModelName, text2)));
				manageMenu.AddMenuItem(item);
				Vehicle activeVehicle = ActiveVehicleScript.ActiveVehicle;
				if ((Entity)(object)activeVehicle != (Entity)null)
				{
					Vector3 position = ((Entity)activeVehicle).Position;
					float num6 = ((Vector3)(ref position)).DistanceToSquared(((Entity)Game.PlayerPed).Position);
					bool flag4 = (Entity)(object)activeVehicle.Driver != (Entity)null && activeVehicle.Driver.Exists() && (Entity)(object)activeVehicle.Driver != (Entity)(object)Game.PlayerPed;
					if (num6 <= 3600f && !flag4)
					{
						flag3 = false;
					}
				}
			}
			if (flag3)
			{
				bool flag5 = !selectedVehicle.IsDead && !selectedVehicle.IsImpounded && !selectedVehicle.IsInMaintenance && !flag2;
				menuItems["summon"] = new MenuItem(LocalizationController.S(Entries.Vehicles.MENU_VEHICLES_DELIVERY_TITLE))
				{
					Description = (flag ? LocalizationController.S(Entries.Vehicles.MENU_VEHICLES_DELIVERY_DESCRIPTION_RENTED, vehicleModelName) : (LocalizationController.S(Entries.Vehicles.MENU_VEHICLES_DELIVERY_DESCRIPTION, vehicleModelName) + (selectedVehicle.IsDead ? ("\n" + LocalizationController.S(Entries.Vehicles.MENU_VEHICLES_DELIVERY_INFO_DESTROYED)) : "") + (selectedVehicle.IsImpounded ? ("\n" + LocalizationController.S(Entries.Vehicles.MENU_VEHICLES_INFO_IMPOUNDED)) : "") + (selectedVehicle.IsInMaintenance ? ("\n" + LocalizationController.S(Entries.Vehicles.MENU_VEHICLES_INFO_IN_MAINTENANCE)) : "") + ((personalVehicleModel?.WasRecalled ?? true) ? ("\n" + LocalizationController.S(Entries.Vehicles.MENU_VEHICLES_DELIVERY_INFO_RECALLED)) : "") + (flag2 ? ("\n" + LocalizationController.S(Entries.Vehicles.MENU_VEHICLES_INFO_RENT_OVERDUE)) : ""))),
					Label = ((!flag5) ? LocalizationController.S(Entries.Main.NOT_AVAILABLE_SHORT) : num3.ToPriceTagString(num)),
					Enabled = (num >= num3 && flag5)
				};
				if (!flag)
				{
					if (garageById != null && (int)currentMembershipTier < (int)garageById.MembershipTier)
					{
						menuItems["summon"].Enabled = false;
						menuItems["summon"].RightIcon = MenuItem.Icon.LOCK;
						MenuItem menuItem2 = menuItems["summon"];
						menuItem2.Description = menuItem2.Description + "\n" + LocalizationController.S(Entries.Vehicles.MENU_VEHICLES_DELIVERY_INFO_GARAGE_MEMBERSHIP, Gtacnr.Utils.GetDescription(garageById.MembershipTier));
					}
					if (personalVehicleModel != null && (int)personalVehicleModel.MembershipTier > 0 && (int)currentMembershipTier < (int)personalVehicleModel.MembershipTier)
					{
						menuItems["summon"].Enabled = false;
						menuItems["summon"].RightIcon = MenuItem.Icon.LOCK;
						MenuItem menuItem3 = menuItems["summon"];
						menuItem3.Description = menuItem3.Description + "\n" + LocalizationController.S(Entries.Vehicles.MENU_VEHICLES_DELIVERY_INFO_MEMBERSHIP, Gtacnr.Utils.GetDescription(personalVehicleModel.MembershipTier));
					}
				}
				ManageMenu.AddMenuItem(menuItems["summon"]);
			}
			bool flag6 = !selectedVehicle.IsDead && !selectedVehicle.IsImpounded && !selectedVehicle.IsInMaintenance && !flag2;
			menuItems["maintenance"] = new MenuItem(LocalizationController.S(Entries.Vehicles.MENU_VEHICLES_MAINTENANCE_TITLE))
			{
				Description = (flag ? LocalizationController.S(Entries.Vehicles.MENU_VEHICLES_MAINTENANCE_DESCRIPTION_RENTED, vehicleModelName) : (LocalizationController.S(Entries.Vehicles.MENU_VEHICLES_MAINTENANCE_DESCRIPTION, vehicleModelName) + (selectedVehicle.IsDead ? ("\n" + LocalizationController.S(Entries.Vehicles.MENU_VEHICLES_MAINTENANCE_INFO_DESTROYED)) : "") + (selectedVehicle.IsImpounded ? ("\n" + LocalizationController.S(Entries.Vehicles.MENU_VEHICLES_INFO_IMPOUNDED)) : "") + (selectedVehicle.IsInMaintenance ? ("\n" + LocalizationController.S(Entries.Vehicles.MENU_VEHICLES_MAINTENANCE_INFO_IN_MAINTENANCE)) : "") + (flag2 ? ("\n" + LocalizationController.S(Entries.Vehicles.MENU_VEHICLES_INFO_RENT_OVERDUE)) : ""))),
				Label = ((!flag6) ? LocalizationController.S(Entries.Main.NOT_AVAILABLE_SHORT) : num4.ToPriceTagString(num)),
				Enabled = (num >= num4 && flag6)
			};
			ManageMenu.AddMenuItem(menuItems["maintenance"]);
			bool isDead = selectedVehicle.IsDead;
			Menu manageMenu2 = ManageMenu;
			item = (menuItems["replace"] = new MenuItem(LocalizationController.S(Entries.Vehicles.MENU_VEHICLES_REPLACE_TITLE))
			{
				Description = LocalizationController.S(Entries.Vehicles.MENU_VEHICLES_REPLACE_DESCRIPTION) + ((!selectedVehicle.IsDead) ? ("\n" + LocalizationController.S(Entries.Vehicles.MENU_VEHICLES_REPLACE_INFO_NOT_NEEDED)) : ""),
				Label = (isDead ? num5.ToPriceTagString(num) : LocalizationController.S(Entries.Main.NOT_AVAILABLE_SHORT)),
				Enabled = (num >= num5 && isDead)
			});
			manageMenu2.AddMenuItem(item);
			if (flag)
			{
				Menu manageMenu3 = ManageMenu;
				item = (menuItems["rental"] = new MenuItem(LocalizationController.S(Entries.Vehicles.MENU_VEHICLES_RENTAL_TITLE))
				{
					Description = LocalizationController.S(Entries.Vehicles.MENU_VEHICLES_RENTAL_DESCRIPTION, selectedVehicle.RentData.StartDateTime.ToFormalDateTime(), selectedVehicle.RentData.EndDateTime.ToFormalDateTime(), selectedVehicle.RentData.RenewPrice.ToCurrencyString()),
					PlaySelectSound = false,
					Label = (flag2 ? LocalizationController.S(Entries.Vehicles.MENU_VEHICLES_RENTAL_INFO_OVERDUE) : (Gtacnr.Utils.CalculateTimeIn(selectedVehicle.RentData.EndDateTime) ?? ""))
				});
				manageMenu3.AddMenuItem(item);
				Menu manageMenu4 = ManageMenu;
				item = (menuItems["rentRenew"] = new MenuItem(LocalizationController.S(Entries.Vehicles.MENU_VEHICLES_RENT_RENEW_TITLE))
				{
					Description = LocalizationController.S(Entries.Vehicles.MENU_VEHICLES_RENT_RENEW_DESCRIPTION, vehicleModelName) + ((!flag2) ? ("\n" + LocalizationController.S(Entries.Vehicles.MENU_VEHICLES_RENT_RENEW_INFO_ONGOING)) : ""),
					Label = (flag2 ? selectedVehicle.RentData.RenewPrice.ToPriceTagString(num) : LocalizationController.S(Entries.Main.NOT_AVAILABLE_SHORT)),
					Enabled = flag2
				});
				manageMenu4.AddMenuItem(item);
				bool enabled = !selectedVehicle.IsDead && !selectedVehicle.IsImpounded && !selectedVehicle.IsInMaintenance;
				Menu manageMenu5 = ManageMenu;
				item = (menuItems["rentEnd"] = new MenuItem(LocalizationController.S(Entries.Vehicles.MENU_VEHICLES_RENT_END_TITLE))
				{
					Description = LocalizationController.S(Entries.Vehicles.MENU_VEHICLES_RENT_END_DESCRIPTION, vehicleModelName) + (selectedVehicle.IsDead ? ("\n" + LocalizationController.S(Entries.Vehicles.MENU_VEHICLES_RENT_END_INFO_DESTROYED)) : "") + (selectedVehicle.IsImpounded ? ("\n" + LocalizationController.S(Entries.Vehicles.MENU_VEHICLES_INFO_IMPOUNDED)) : "") + (selectedVehicle.IsInMaintenance ? ("\n" + LocalizationController.S(Entries.Vehicles.MENU_VEHICLES_INFO_IN_MAINTENANCE)) : "") + ((!flag2) ? ("\n" + LocalizationController.S(Entries.Vehicles.MENU_VEHICLES_RENT_END_INFO_ONGOING)) : ""),
					Enabled = enabled
				});
				manageMenu5.AddMenuItem(item);
				return;
			}
			Menu manageMenu6 = ManageMenu;
			item = (menuItems["history"] = new MenuItem(LocalizationController.S(Entries.Vehicles.MENU_VEHICLES_HISTORY_TITLE))
			{
				Description = LocalizationController.S(Entries.Vehicles.MENU_VEHICLES_HISTORY_DESCRIPTION),
				Label = Utils.MENU_ARROW
			});
			manageMenu6.AddMenuItem(item);
			MenuController.BindMenuItem(ManageMenu, HistoryMenu, menuItems["history"]);
			HistoryMenu.ClearMenuItems();
			int num7 = dealershipSupply?.Price ?? serviceInfo.Sell.DefaultPrice;
			VehicleOwnershipData ownershipData = selectedVehicle.OwnershipData;
			if (ownershipData != null && ownershipData.History?.Count > 0)
			{
				int num8 = 0;
				foreach (VehicleOwnershipDataEntry item3 in selectedVehicle.OwnershipData?.History)
				{
					item = new MenuItem(item3.PurchaseDateTime.Value.ToFormalDate2() ?? "");
					item.Description = LocalizationController.S(Entries.Vehicles.MENU_VEHICLES_HISTORY_ENTRY_DESCRIPTION, item3.PurchaseDateTime.Value.ToFormalDate2(), item3.PurchasePrice.ToCurrencyString());
					item.Label = "~g~" + item3.PurchasePrice.ToCurrencyString();
					MenuItem item2 = item;
					HistoryMenu.AddMenuItem(item2);
					if (num8 == 0)
					{
						num7 = item3.PurchasePrice;
					}
					num8++;
				}
			}
			else
			{
				HistoryMenu.AddMenuItem(new MenuItem(LocalizationController.S(Entries.Vehicles.MENU_VEHICLES_HISTORY_EMPTY_TITLE), LocalizationController.S(Entries.Vehicles.MENU_VEHICLES_HISTORY_EMPTY_DESCRIPTION)));
			}
			if (!(personalVehicleModel?.WasRecalled ?? true))
			{
				num7 = serviceInfo.Sell.CalculatePrice(num7);
			}
			sellPriceCache = num7;
			menuItems["sell"] = new MenuItem(LocalizationController.S(Entries.Vehicles.MENU_VEHICLES_SELL_TITLE))
			{
				Description = LocalizationController.S(Entries.Vehicles.MENU_VEHICLES_SELL_DESCRIPTION, vehicleModelName),
				Label = "~g~" + num7.ToCurrencyString()
			};
			ManageMenu.AddMenuItem(menuItems["sell"]);
		}
		catch (Exception ex)
		{
			Print(ex);
			CarsMenu.ClearMenuItems();
			CarsMenu.AddErrorMenuItem(ex);
		}
	}

	private bool PrepareSummonPosition()
	{
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		int num = 5;
		bool success = false;
		Vector3 spawnPos = default(Vector3);
		float spawnHeading = 0f;
		Vector3 playerPedPos = ((Entity)Game.PlayerPed).Position;
		valetSpawnCoords = default(Vector4);
		while (!GetNode() && num > 0)
		{
			num--;
		}
		return success;
		bool GetNode()
		{
			//IL_0073: Unknown result type (might be due to invalid IL or missing references)
			//IL_00de: Unknown result type (might be due to invalid IL or missing references)
			//IL_00e3: Unknown result type (might be due to invalid IL or missing references)
			success = API.GetNthClosestVehicleNodeFavourDirection(playerPedPos.X, playerPedPos.Y, playerPedPos.Z, playerPedPos.X, playerPedPos.Y, playerPedPos.Z, 30 + random.Next(20), ref spawnPos, ref spawnHeading, 1, 1077936128, 0) && ((Vector3)(ref spawnPos)).DistanceToSquared2D(playerPedPos) <= 360000f && Math.Abs(playerPedPos.Z - spawnPos.Z) < 3f;
			if (success)
			{
				valetSpawnCoords = new Vector4(spawnPos.X, spawnPos.Y, spawnPos.Z, spawnHeading);
			}
			return success;
		}
	}

	private async void SummonVehicle(StoredVehicle storedVehicle)
	{
		_ = 7;
		try
		{
			if (currentSummonStoredVehicle != null)
			{
				Utils.DisplayErrorMessage(92, 5, LocalizationController.S(Entries.Vehicles.VEHICLE_ALREADY_IN_DELIVERY));
				Utils.PlayErrorSound();
				return;
			}
			currentSummonStoredVehicle = storedVehicle;
			JobsEnum job = Gtacnr.Client.API.Jobs.CachedJobEnum;
			JobsEnum initialJob = job;
			string vehicleFullName = Utils.GetVehicleFullName(storedVehicle.Model);
			bool prank = false;
			if (AprilsFoolsScript.IsAprilsFools && Gtacnr.Utils.GetRandomDouble() < 0.25)
			{
				List<string> collection = new List<string> { "speedo2", "mower", "tractor3", "ripley", "fixter", "issi6" };
				storedVehicle.Model = API.GetHashKey(collection.Random());
				storedVehicle.ModData = new VehicleModData();
				prank = true;
			}
			if (valetSpawnCoords == default(Vector4))
			{
				Utils.DisplayErrorMessage(92, 2);
				BaseScript.TriggerServerEvent("gtacnr:vehicles:summonFailed", new object[0]);
				return;
			}
			Vehicle vehicle = await Utils.CreateStoredVehicle(storedVehicle, valetSpawnCoords.XYZ(), valetSpawnCoords.W);
			CurrentSummonVehicle = vehicle;
			int attempts = 1;
			if ((Entity)(object)vehicle == (Entity)null)
			{
				while (true)
				{
					Utils.DisplayHelpText(LocalizationController.S(Entries.Player.UNABLE_TO_DOWNLOAD_VEHICLE_MODEL, $"{attempts}/{3}"));
					await BaseScript.Delay(5000);
					vehicle = await Utils.CreateStoredVehicle(storedVehicle, valetSpawnCoords.XYZ(), valetSpawnCoords.W);
					if ((Entity)(object)vehicle != (Entity)null)
					{
						break;
					}
					attempts++;
					if (attempts > 3)
					{
						Utils.DisplayHelpText(LocalizationController.S(Entries.Player.UNABLE_TO_DOWNLOAD_VEHICLE_MODEL_5C_3));
						BaseScript.TriggerServerEvent("gtacnr:vehicles:summonFailed", new object[0]);
						return;
					}
				}
			}
			vehicle.LockStatus = (VehicleLockStatus)10;
			vehicle.CanBeVisiblyDamaged = false;
			if (!(await AntiEntitySpawnScript.RegisterEntity((Entity)(object)vehicle)))
			{
				((PoolObject)vehicle).Delete();
				Utils.DisplayHelpText(LocalizationController.S(Entries.Player.UNABLE_TO_DOWNLOAD_VEHICLE_MODEL_5C_4));
				BaseScript.TriggerServerEvent("gtacnr:vehicles:summonFailed", new object[0]);
				return;
			}
			if ((Entity)(object)valet != (Entity)null && valet.Exists())
			{
				((PoolObject)valet).Delete();
			}
			string text = "s_m_y_winclean_01";
			if (AprilsFoolsScript.IsAprilsFools)
			{
				switch (Gtacnr.Utils.GetRandomInt(0, 8))
				{
				case 0:
					text = "a_m_y_acult_02";
					break;
				case 1:
					text = "s_m_y_clown_01";
					break;
				case 2:
					text = "a_m_m_beach_01";
					break;
				case 3:
					text = "a_f_m_fatcult_01";
					break;
				case 4:
					text = "u_m_y_danceburl_01";
					break;
				case 5:
					text = "shrek";
					break;
				case 6:
					text = "ig_orleans";
					break;
				case 7:
					text = "a_m_m_tranvest_01";
					break;
				}
			}
			else if (job.IsPolice())
			{
				text = ((random.Next(10) < 5) ? "s_m_y_cop_01" : "s_f_y_cop_01");
				DealershipSupply dealershipSupply = DealershipScript.FindFirstSupplyOfModel(storedVehicle.Model);
				if (dealershipSupply != null)
				{
					if (dealershipSupply.Price >= 8000000)
					{
						text = "mp_m_fibsec_01";
					}
					else if (dealershipSupply.Price >= 4000000)
					{
						text = "s_m_y_swat_01";
					}
				}
			}
			else
			{
				switch (job)
				{
				case JobsEnum.Paramedic:
					text = "s_m_m_paramedic_01";
					break;
				case JobsEnum.DeliveryDriver:
					text = "s_m_m_postal_02";
					break;
				default:
				{
					DealershipSupply dealershipSupply2 = DealershipScript.FindFirstSupplyOfModel(storedVehicle.Model);
					if (dealershipSupply2 != null)
					{
						if (dealershipSupply2.Price >= 8000000)
						{
							text = "u_f_m_debbie_01";
						}
						else if (dealershipSupply2.Price >= 4000000)
						{
							text = "s_m_y_westsec_01";
						}
						else if (dealershipSupply2.Price >= 2000000)
						{
							text = "s_m_y_valet_01";
						}
						else if (dealershipSupply2.Price >= 1000000)
						{
							text = "s_m_m_gentransport";
						}
					}
					break;
				}
				}
			}
			valet = await vehicle.CreatePedOnSeat((VehicleSeat)(-1), Model.op_Implicit(text));
			if (!valet.IsInVehicle(vehicle))
			{
				valet.Task.WarpIntoVehicle(vehicle, (VehicleSeat)(-1));
			}
			API.SetPedRandomComponentVariation(((PoolObject)valet).Handle, false);
			valet.AlwaysKeepTask = true;
			valet.BlockPermanentEvents = true;
			API.SetPedRelationshipGroupHash(((PoolObject)valet).Handle, (uint)API.GetHashKey("serviceNpcs"));
			await AntiEntitySpawnScript.RegisterEntity((Entity)(object)valet);
			BaseScript.TriggerServerEvent("gtacnr:entities:tempEntitiesCreated", new object[1] { new List<int> { ((Entity)valet).NetworkId }.Json() });
			int drivingStyle = 537133948;
			Vector3 initialCallPosition = ((Entity)Game.PlayerPed).Position;
			Vector3 callPosition = ((Entity)Game.PlayerPed).Position;
			valet.Task.DriveTo(vehicle, callPosition, 8f, 17f, drivingStyle);
			API.SetDriverAbility(((PoolObject)valet).Handle, 1f);
			API.SetDriverAggressiveness(((PoolObject)valet).Handle, 0.2f);
			DateTime deliveryStartTime = DateTime.UtcNow;
			string err = LocalizationController.S(Entries.Player.UNABLE_TO_DELIVER_VEHICLE);
			while (true)
			{
				await BaseScript.Delay(500);
				if (Gtacnr.Client.API.Jobs.CachedJobEnum != initialJob)
				{
					Utils.DisplayHelpText(LocalizationController.S(Entries.Player.CHANGED_JOBS_DELIVERY_CANCELED));
					Cancel();
					return;
				}
				if (((Vector3)(ref callPosition)).DistanceToSquared(((Entity)Game.PlayerPed).Position) > 400f)
				{
					valet.Task.ClearAll();
					callPosition = ((Entity)Game.PlayerPed).Position;
					valet.Task.DriveTo(vehicle, callPosition, 8f, 17f, drivingStyle);
				}
				if (((Vector3)(ref initialCallPosition)).DistanceToSquared(((Entity)Game.PlayerPed).Position) > 22500f)
				{
					Utils.DisplayHelpText(LocalizationController.S(Entries.Player.AREA_LEFT_DELIVERY_CANCELED));
					Cancel();
					return;
				}
				if (!((Entity)valet).IsAlive)
				{
					Utils.DisplayHelpText(LocalizationController.S(Entries.Player.VALET_KILLED_DELIVERY_CANCELED));
					Cancel();
					return;
				}
				Vector3 position = ((Entity)vehicle).Position;
				Vector3 position2 = ((Entity)Game.PlayerPed).Position;
				if (((Vector3)(ref position)).DistanceToSquared(position2) < 400f)
				{
					break;
				}
				if (Gtacnr.Utils.CheckTimePassed(deliveryStartTime, TimeSpan.FromSeconds(30.0)))
				{
					Vector3 position3 = default(Vector3);
					float heading = 0f;
					if (!API.GetClosestVehicleNodeWithHeading(position2.X, position2.Y, position2.Z, ref position3, ref heading, 1, 3f, 0))
					{
						Utils.DisplayHelpText(LocalizationController.S(Entries.Player.TOO_FAR_FROM_ROAD_DELIVERY_CANCELED));
						Cancel();
						return;
					}
					((Entity)vehicle).Position = position3;
					((Entity)vehicle).Heading = heading;
					break;
				}
			}
			valet.Task.ClearAll();
			valet.Task.LeaveVehicle((LeaveVehicleFlags)0);
			valet.AlwaysKeepTask = false;
			valet.BlockPermanentEvents = false;
			valet.PlayAmbientSpeech("GENERIC_HOWS_IT_GOING", (SpeechModifier)3);
			vehicle.Doors[(VehicleDoorIndex)0].Close(false);
			vehicle.LockStatus = (VehicleLockStatus)2;
			vehicle.CanBeVisiblyDamaged = true;
			if (prank)
			{
				Utils.DisplaySubtitle(LocalizationController.S(Entries.Vehicles.VEHICLE_DELIVERED_PRANK));
			}
			else
			{
				Utils.DisplaySubtitle(LocalizationController.S(Entries.Vehicles.VEHICLE_DELIVERED, vehicleFullName));
			}
			if (!(await ActiveVehicleScript.SetActiveVehicle(storedVehicle)))
			{
				Utils.DisplayErrorMessage(68, 3, err);
			}
			FinalizeValetAsync(valet);
			void Cancel()
			{
				((PoolObject)vehicle).Delete();
				((PoolObject)valet).Delete();
			}
		}
		catch (Exception exception)
		{
			Print(exception);
			Utils.DisplayErrorMessage(68, 2, LocalizationController.S(Entries.Main.UNEXPECTED_ERROR_PRESS_F8));
		}
		finally
		{
			currentSummonStoredVehicle = null;
			CurrentSummonVehicle = null;
		}
	}

	private static async void FinalizeValetAsync(Ped valet)
	{
		await BaseScript.Delay(1000);
		valet.Task.WanderAround();
		await BaseScript.Delay(60000);
		((PoolObject)valet).Delete();
	}

	public static void InvalidateCache()
	{
		vehiclesCache = null;
	}

	public static void AddExtraVehicle(string job, string vehicleName)
	{
		if (!extraVehicles.ContainsKey(job))
		{
			extraVehicles[job] = new List<string>();
		}
		if (!extraVehicles[job].Contains(vehicleName))
		{
			extraVehicles[job].Add(vehicleName);
		}
	}

	[EventHandler("gtacnr:vehicles:healthUpdated")]
	private void OnHealthUpdated(string storedVehId, string jHealthData, bool maintenance)
	{
		StoredVehicle storedVehicle = vehiclesCache.FirstOrDefault((StoredVehicle v) => v.Id == storedVehId);
		if (storedVehicle == null)
		{
			return;
		}
		storedVehicle.IsInMaintenance = false;
		storedVehicle.HealthData = jHealthData.Unjson<VehicleHealthData>();
		if (maintenance)
		{
			string text = Utils.GetVehicleFullName(storedVehicle.Model);
			if (string.IsNullOrEmpty(text))
			{
				text = LocalizationController.S(Entries.Player.VEHICLE_MAINTENANCE_COMPLETED_INVALID_NAME);
			}
			Utils.DisplayHelpText(LocalizationController.S(Entries.Vehicles.VEHICLE_COMPLETED_MAINTENANCE, text));
		}
		RefreshMenu();
		RefreshManageMenu();
	}

	public static bool ResolveSummonVehicleResponse(SummonVehicleResponse response, int errorCode)
	{
		switch (response)
		{
		case SummonVehicleResponse.Success:
			return true;
		case SummonVehicleResponse.NoMoney:
			Utils.DisplayHelpText(LocalizationController.S(Entries.Main.NOT_ENOUGH_MONEY_BANK_ACCOUNT));
			break;
		case SummonVehicleResponse.InvalidRoutingBucket:
			Utils.DisplayHelpText(LocalizationController.S(Entries.Player.CANT_DELIVER_HERE));
			break;
		case SummonVehicleResponse.JobMismatch:
		{
			string text = Gtacnr.Data.Jobs.GetJobData(Gtacnr.Client.API.Jobs.CachedJob)?.GetColoredName() ?? "N/A";
			Utils.DisplayHelpText(LocalizationController.S(Entries.Player.CANT_GET_DELIVERED_AS_JOB, text));
			break;
		}
		case SummonVehicleResponse.AttachedToTowtruck:
			Utils.DisplayHelpText(LocalizationController.S(Entries.Player.VEHICLE_ATTACHED_TO_TOW_TRUCK));
			break;
		case SummonVehicleResponse.RentOverdue:
			Utils.DisplayHelpText(LocalizationController.S(Entries.Player.VEHICLE_RENT_EXPIRED));
			break;
		case SummonVehicleResponse.Unavailable:
			Utils.DisplayHelpText(LocalizationController.S(Entries.Player.UNABLE_TO_DELIVER_VEHICLE));
			break;
		default:
			Utils.DisplayErrorMessage(errorCode, (int)response);
			break;
		case SummonVehicleResponse.Cooldown:
			break;
		}
		return false;
	}
}
