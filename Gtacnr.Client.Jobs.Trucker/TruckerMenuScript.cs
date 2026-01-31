using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Gtacnr.Client.API;
using Gtacnr.Client.Businesses;
using Gtacnr.Client.Businesses.Dealerships;
using Gtacnr.Data;
using Gtacnr.Localization;
using Gtacnr.Model;
using Gtacnr.Model.Enums;
using MenuAPI;

namespace Gtacnr.Client.Jobs.Trucker;

public class TruckerMenuScript : Script
{
	private class PastDeliveryInfo
	{
		public DeliveryJob DeliveryJob { get; set; }

		public GameTime StartGameTime { get; set; }

		public GameTime EndGameTime { get; set; }

		public MenuItem ToMenuItem()
		{
			string description = Gtacnr.Utils.GetDescription(Constants.DeliveryDriver.GetRequiredVehicleType(DeliveryJob.Type));
			Tuple<string, string> menuItemTitleAndPrefix = DeliveryJob.GetMenuItemTitleAndPrefix();
			MenuItem menuItem = new MenuItem(menuItemTitleAndPrefix.Item1);
			menuItem.Label = "~g~" + DeliveryJob.PaymentAmount.ToCurrencyString();
			menuItem.Description = menuItemTitleAndPrefix.Item2 + $"Started: ~b~{StartGameTime}~s~\n" + $"Ended: ~b~{EndGameTime}~s~\n" + "Picked up at: ~b~" + DeliveryJob.PickUpLocation.Name + "~s~\nDelivered to: ~b~" + DeliveryJob.GetDeliveryLocationString() + " ~s~(~y~" + DeliveryJob.GetTotalDistanceString() + "~s~)\n" + $"Weight: ~b~{DeliveryJob.Weight / 1000f:0.00} tons~s~\n" + "Truck Type: ~b~" + description + "~s~";
			return menuItem;
		}
	}

	private enum DeliveriesMenuSortMode
	{
		Payout,
		PickupDistance
	}

	private static Menu jobMenu;

	private static Dictionary<string, Menu> subMenus = new Dictionary<string, Menu>();

	private static Dictionary<string, MenuItem> menuItems = new Dictionary<string, MenuItem>();

	private static List<PastDeliveryInfo> pastDeliveries = new List<PastDeliveryInfo>();

	private static TruckerMenuScript instance;

	private DeliveriesMenuSortMode deliveriesMenuSortMode;

	private bool isBusy;

	private List<DeliveryJob> deliveryJobCache;

	private DateTime manualRefreshTimestamp;

	public static Menu Menu => jobMenu;

	public static IEnumerable<DeliveryJob> DeliveryJobCache => instance.deliveryJobCache;

	public TruckerMenuScript()
	{
		instance = this;
		jobMenu = new Menu(LocalizationController.S(Entries.Jobs.MENU_DDRIVER_TITLE), LocalizationController.S(Entries.Main.MENU_CHOOSE_OPTION));
		jobMenu.OnItemSelect += OnMenuItemSelect;
		menuItems["currentDelivery"] = new MenuItem("~b~" + LocalizationController.S(Entries.Jobs.MENU_DDRIVER_CURRENT_DELIVERY_TEXT), LocalizationController.S(Entries.Jobs.MENU_DDRIVER_CURRENT_DELIVERY_DESCR))
		{
			LeftIcon = MenuItem.Icon.GTACNR_JOB
		};
		subMenus["currentDelivery"] = new Menu(LocalizationController.S(Entries.Jobs.MENU_DDRIVER_CURRENT_DELIVERY_TEXT), LocalizationController.S(Entries.Jobs.MENU_DDRIVER_CURRENT_DELIVERY_SUBTITLE));
		subMenus["currentDelivery"].OnMenuOpen += OnMenuOpened;
		subMenus["currentDelivery"].OnItemSelect += OnMenuItemSelect;
		Menu menu = subMenus["currentDelivery"];
		Dictionary<string, MenuItem> dictionary = menuItems;
		MenuItem obj = new MenuItem(LocalizationController.S(Entries.Jobs.MENU_DDRIVER_DELIVERY_DETAILS_PAYOUT))
		{
			PlaySelectSound = false
		};
		MenuItem item = obj;
		dictionary["deliveryPayout"] = obj;
		menu.AddMenuItem(item);
		Menu menu2 = subMenus["currentDelivery"];
		Dictionary<string, MenuItem> dictionary2 = menuItems;
		MenuItem obj2 = new MenuItem(LocalizationController.S(Entries.Jobs.MENU_DDRIVER_DELIVERY_DETAILS_TIME_LEFT))
		{
			PlaySelectSound = false
		};
		item = obj2;
		dictionary2["deliveryTimeLeft"] = obj2;
		menu2.AddMenuItem(item);
		Menu menu3 = subMenus["currentDelivery"];
		Dictionary<string, MenuItem> dictionary3 = menuItems;
		MenuItem obj3 = new MenuItem(LocalizationController.S(Entries.Jobs.MENU_DDRIVER_DELIVERY_DETAILS_ITINERARY))
		{
			PlaySelectSound = false
		};
		item = obj3;
		dictionary3["deliveryItinerary"] = obj3;
		menu3.AddMenuItem(item);
		Menu menu4 = subMenus["currentDelivery"];
		Dictionary<string, MenuItem> dictionary4 = menuItems;
		MenuItem obj4 = new MenuItem(LocalizationController.S(Entries.Jobs.MENU_DDRIVER_DELIVERY_DETAILS_OTHER))
		{
			PlaySelectSound = false
		};
		item = obj4;
		dictionary4["deliveryOtherDetails"] = obj4;
		menu4.AddMenuItem(item);
		subMenus["currentDelivery"].AddMenuItem(menuItems["cancelDelivery"] = new MenuItem(LocalizationController.S(Entries.Jobs.MENU_DDRIVER_DELIVERY_CANCEL), LocalizationController.S(Entries.Jobs.MENU_DDRIVER_DELIVERY_CANCEL_DESC)));
		MenuController.AddSubmenu(jobMenu, subMenus["currentDelivery"]);
		MenuController.BindMenuItem(jobMenu, subMenus["currentDelivery"], menuItems["currentDelivery"]);
		Menu menu5 = jobMenu;
		Dictionary<string, MenuItem> dictionary5 = menuItems;
		MenuItem obj5 = new MenuItem(LocalizationController.S(Entries.Jobs.MENU_DDRIVER_DELIVERIES_TEXT), LocalizationController.S(Entries.Jobs.MENU_DDRIVER_DELIVERIES_DESCR))
		{
			LeftIcon = MenuItem.Icon.GTACNR_JOB_CALLS
		};
		item = obj5;
		dictionary5["deliveries"] = obj5;
		menu5.AddMenuItem(item);
		subMenus["deliveries"] = new Menu(LocalizationController.S(Entries.Jobs.MENU_DDRIVER_DELIVERIES_TEXT));
		subMenus["deliveries"].OnMenuOpen += OnMenuOpened;
		subMenus["deliveries"].OnItemSelect += OnMenuItemSelect;
		subMenus["deliveries"].OnIndexChange += OnMenuIndexChange;
		MenuController.AddSubmenu(jobMenu, subMenus["deliveries"]);
		MenuController.BindMenuItem(jobMenu, subMenus["deliveries"], menuItems["deliveries"]);
		subMenus["deliveries"].InstructionalButtons.Clear();
		subMenus["deliveries"].InstructionalButtons.Add((Control)201, LocalizationController.S(Entries.Main.BTN_SELECT));
		subMenus["deliveries"].InstructionalButtons.Add((Control)202, LocalizationController.S(Entries.Main.BTN_BACK));
		subMenus["deliveries"].InstructionalButtons.Add((Control)205, LocalizationController.S(Entries.Main.BTN_SORT));
		subMenus["deliveries"].InstructionalButtons.Add((Control)166, LocalizationController.S(Entries.Main.BTN_REFRESH));
		subMenus["deliveries"].ButtonPressHandlers.AddRange(new Menu.ButtonPressHandler[2]
		{
			new Menu.ButtonPressHandler((Control)205, MenuAPI.Menu.ControlPressCheckType.JUST_PRESSED, OnDeliveriesMenuSort, disableControl: true),
			new Menu.ButtonPressHandler((Control)166, MenuAPI.Menu.ControlPressCheckType.JUST_PRESSED, OnDeliveriesMenuRefresh, disableControl: true)
		});
		Menu menu6 = jobMenu;
		Dictionary<string, MenuItem> dictionary6 = menuItems;
		MenuItem obj6 = new MenuItem(LocalizationController.S(Entries.Jobs.MENU_DDRIVER_HISTORY_TEXT), LocalizationController.S(Entries.Jobs.MENU_DDRIVER_HISTORY_DESCR))
		{
			LeftIcon = MenuItem.Icon.GTACNR_JOB_SALES
		};
		item = obj6;
		dictionary6["history"] = obj6;
		menu6.AddMenuItem(item);
		subMenus["history"] = new Menu(LocalizationController.S(Entries.Jobs.MENU_DDRIVER_HISTORY_TEXT), LocalizationController.S(Entries.Jobs.MENU_DDRIVER_HISTORY_SUBTITLE));
		subMenus["history"].PlaySelectSound = false;
		subMenus["history"].OnMenuOpen += OnMenuOpened;
		MenuController.AddSubmenu(jobMenu, subMenus["history"]);
		MenuController.BindMenuItem(jobMenu, subMenus["history"], menuItems["history"]);
	}

	protected override void OnStarted()
	{
		ShoppingScript.AddExternalItemSelectHandler(OnMenuItemSelect);
		ShoppingScript.AddExternalMenuOpenHandler(BusinessType.DeliveryCompany, OnBusinessMenuOpen);
	}

	[EventHandler("gtacnr:spawned")]
	private async void OnSpawned()
	{
		RefreshBusinessMenu();
		Gtacnr.Client.API.Jobs.JobChangedEvent += OnJobChangedEvent;
		Users.XPChanged += OnXPChanged;
	}

	private void OnJobChangedEvent(object sender, JobArgs e)
	{
		RefreshBusinessMenu();
	}

	private void OnXPChanged(object sender, Users.XPEventArgs e)
	{
		RefreshBusinessMenu();
	}

	public static void AddPastDelivery(DeliveryJob delivery, GameTime startGameTime, GameTime endGameTime)
	{
		pastDeliveries.Add(new PastDeliveryInfo
		{
			DeliveryJob = delivery,
			StartGameTime = startGameTime,
			EndGameTime = endGameTime
		});
	}

	private void RefreshBusinessMenu()
	{
		if (menuItems.ContainsKey("toCivilian"))
		{
			ShoppingScript.RemoveExternalMenuItem(BusinessType.DeliveryCompany, menuItems["toCivilian"]);
		}
		if (menuItems.ContainsKey("toDriver"))
		{
			ShoppingScript.RemoveExternalMenuItem(BusinessType.DeliveryCompany, menuItems["toDriver"]);
		}
		if (menuItems.ContainsKey("vehicles"))
		{
			ShoppingScript.RemoveExternalMenuItem(BusinessType.DeliveryCompany, menuItems["vehicles"]);
		}
		menuItems["toCivilian"] = Gtacnr.Data.Jobs.GetJobData("none").ToSwitchMenuItem();
		menuItems["toDriver"] = Gtacnr.Data.Jobs.GetJobData("deliveryDriver").ToSwitchMenuItem();
		menuItems["vehicles"] = new MenuItem("Vehicles", "Purchase ~y~vehicles ~s~that you can use when you're working as a delivery driver.")
		{
			Label = Utils.MENU_ARROW
		};
		if (Gtacnr.Client.API.Jobs.CachedJob == "deliveryDriver")
		{
			Dealership closestDealership = DealershipScript.GetClosestDealership();
			if (closestDealership != null && closestDealership.Type == DealershipType.DeliveryCompany)
			{
				ShoppingScript.AddExternalMenuItem(BusinessType.DeliveryCompany, menuItems["vehicles"]);
			}
			ShoppingScript.AddExternalMenuItem(BusinessType.DeliveryCompany, menuItems["toCivilian"]);
		}
		else
		{
			ShoppingScript.AddExternalMenuItem(BusinessType.DeliveryCompany, menuItems["toDriver"]);
		}
	}

	private void OnBusinessMenuOpen(Menu menu)
	{
		RefreshBusinessMenu();
	}

	public static void RefreshCurrentDeliveryMenu()
	{
		//IL_016e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0179: Unknown result type (might be due to invalid IL or missing references)
		//IL_0184: Unknown result type (might be due to invalid IL or missing references)
		//IL_0190: Unknown result type (might be due to invalid IL or missing references)
		//IL_019c: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d8: Unknown result type (might be due to invalid IL or missing references)
		if (TruckerJobScript.CurrentDelivery == null)
		{
			return;
		}
		subMenus["currentDelivery"].MenuSubtitle = LocalizationController.S(Entries.Jobs.MENU_DDRIVER_CURRENT_DELIVERY_SUBTITLE, TruckerJobScript.CurrentDelivery.DropOffLocations.Last().Name);
		menuItems["deliveryPayout"].Label = "~g~" + TruckerJobScript.CurrentDelivery.PaymentAmount.ToCurrencyString();
		TimeSpan deliveryTimeLeft = TruckerJobScript.CurrentDelivery.GetDeliveryTimeLeft();
		menuItems["deliveryTimeLeft"].Label = ((deliveryTimeLeft.TotalHours > 0.0) ? $"{Math.Floor(deliveryTimeLeft.TotalHours):00}:{deliveryTimeLeft.Minutes:00}" : ("~r~" + LocalizationController.S(Entries.Main.MISSION_LATE)));
		menuItems["deliveryItinerary"].Description = TruckerJobScript.CurrentDelivery.PickUpLocation.Name + "\n";
		DeliveryJobLocation deliveryJobLocation = TruckerJobScript.CurrentDelivery.PickUpLocation;
		float num = 0f;
		foreach (DeliveryJobLocation dropOffLocation in TruckerJobScript.CurrentDelivery.DropOffLocations)
		{
			MenuItem menuItem = menuItems["deliveryItinerary"];
			menuItem.Description = menuItem.Description + dropOffLocation.Name + "\n";
			float num2 = API.CalculateTravelDistanceBetweenPoints(deliveryJobLocation.Coordinates.X, deliveryJobLocation.Coordinates.Y, deliveryJobLocation.Coordinates.Z, dropOffLocation.Coordinates.X, dropOffLocation.Coordinates.Y, dropOffLocation.Coordinates.Z);
			if (num2 == 100000f)
			{
				Vector3 val = deliveryJobLocation.Coordinates.XYZ();
				num2 = (float)Math.Sqrt(((Vector3)(ref val)).DistanceToSquared2D(dropOffLocation.Coordinates.XYZ()));
			}
			deliveryJobLocation = dropOffLocation;
			num += num2;
		}
		string text = ((API.GetProfileSetting(227) == 1) ? $"{num.ToKm():0.00}km" : $"{num.ToMiles():0.00}mi");
		MenuItem menuItem2 = menuItems["deliveryItinerary"];
		menuItem2.Description = menuItem2.Description + "~b~Total distance: ~s~" + text;
		menuItems["deliveryItinerary"].Label = text;
		menuItems["deliveryOtherDetails"].Description = $"Weight: ~b~{TruckerJobScript.CurrentDelivery.Weight / 1000f:0.00} tons~s~\n" + "Goods Value: ~b~" + TruckerJobScript.CurrentDelivery.Value.ToCurrencyString() + "~s~\nTruck Type: ~b~" + Gtacnr.Utils.GetDescription(Constants.DeliveryDriver.GetRequiredVehicleType(TruckerJobScript.CurrentDelivery.Type)) + "~s~";
		long amount = ((float)TruckerJobScript.CurrentDelivery.PaymentAmount * ((TruckerJobScript.CurrentDeliveryIndex == -1) ? 0.05f : 0.3f)).ToLongCeil();
		menuItems["cancelDelivery"].Label = "~r~" + amount.ToCurrencyString();
		menuItems["cancelDelivery"].Description = LocalizationController.S(Entries.Jobs.MENU_DDRIVER_DELIVERY_CANCEL_DESC, amount.ToCurrencyString());
	}

	private async void OnMenuItemSelect(Menu menu, MenuItem menuItem, int itemIndex)
	{
		if (menuItem == menuItems["toCivilian"])
		{
			if (!isBusy)
			{
				await Gtacnr.Client.API.Jobs.TrySwitch("none", "default", null, BeforeSwitching, AfterSwitching);
			}
		}
		else if (menuItem == menuItems["toDriver"])
		{
			if (!isBusy)
			{
				await Gtacnr.Client.API.Jobs.TrySwitch("deliveryDriver", "default", LocalizationController.S(Entries.Jobs.DDRIVER_DESCRIPTION), BeforeSwitching, AfterSwitching);
			}
		}
		else if (menuItem == menuItems["vehicles"])
		{
			Dealership closestDealership = DealershipScript.GetClosestDealership();
			if (closestDealership != null && closestDealership.Type == DealershipType.DeliveryCompany)
			{
				MenuController.CloseAllMenus();
				DealershipMenuScript.OpenMenu(closestDealership);
			}
		}
		else if (menuItem.ItemData is DeliveryJob deliveryJob)
		{
			DeliveryJobVehicleType requiredVehicleType = Constants.DeliveryDriver.GetRequiredVehicleType(deliveryJob.Type);
			DeliveryJobVehicleType? truckType = TruckerJobScript.GetTruckType(Game.PlayerPed.CurrentVehicle);
			if (!truckType.HasValue || !requiredVehicleType.HasFlag(truckType))
			{
				Utils.DisplaySubtitle(LocalizationController.S(Entries.Jobs.DDRIVER_JOB_NEED_TRUCK_TYPE, Gtacnr.Utils.GetDescription(requiredVehicleType)));
			}
			else
			{
				MenuController.CloseAllMenus();
				await TruckerJobScript.AssignJob(deliveryJob);
			}
		}
		else if (menuItem == menuItems["cancelDelivery"])
		{
			MenuController.CloseAllMenus();
			TruckerJobScript.ResetDeliveryJob();
			BaseScript.TriggerServerEvent("gtacnr:trucker:cancelCurrentDelivery", new object[0]);
			Utils.DisplaySubtitle(LocalizationController.S(Entries.Jobs.DDRIVER_JOB_CANCELED_BY_USER));
		}
		async Task AfterSwitching()
		{
			await Utils.FadeIn(500);
			MenuController.CloseAllMenus();
			isBusy = false;
		}
		async Task BeforeSwitching()
		{
			isBusy = true;
			MenuController.CloseAllMenus();
			await Utils.FadeOut(500);
		}
	}

	private void OnMenuOpened(Menu menu)
	{
		if (menu == subMenus["deliveries"])
		{
			_RefreshDeliveriesMenu();
		}
		else if (menu == subMenus["history"])
		{
			_RefreshHistoryMenu();
		}
		else if (menu == subMenus["currentDelivery"])
		{
			RefreshCurrentDeliveryMenu();
		}
	}

	private void OnMenuIndexChange(Menu menu, MenuItem oldItem, MenuItem newItem, int oldIndex, int newIndex)
	{
		if (menu == subMenus["deliveries"])
		{
			RefreshMenuItemCountdown(newItem);
		}
	}

	public static void AddCurrentDeliveryMenuItem()
	{
		jobMenu.InsertMenuItem(menuItems["currentDelivery"], 0);
		if (TruckerJobScript.CurrentDelivery != null)
		{
			long amount = ((float)TruckerJobScript.CurrentDelivery.PaymentAmount * ((TruckerJobScript.CurrentDeliveryIndex == -1) ? 0.05f : 0.3f)).ToLongCeil();
			menuItems["cancelDelivery"].Label = "~r~" + amount.ToCurrencyString();
			menuItems["cancelDelivery"].Description = LocalizationController.S(Entries.Jobs.MENU_DDRIVER_DELIVERY_CANCEL_DESC, amount.ToCurrencyString());
		}
	}

	public static void RemoveCurrentDeliveryMenuItem()
	{
		jobMenu.RemoveMenuItem(menuItems["currentDelivery"]);
	}

	private async Task _ReloadAvailableJobs()
	{
		deliveryJobCache = (await TriggerServerEventAsync<string>("gtacnr:trucker:getAvailableJobs", new object[0])).Unjson<List<DeliveryJob>>();
	}

	public static async Task ReloadAvailableJobs()
	{
		await instance._ReloadAvailableJobs();
	}

	private async void _RefreshDeliveriesMenu(bool forceReload = true)
	{
		Menu menu = subMenus["deliveries"];
		if (forceReload || deliveryJobCache == null)
		{
			menu.ClearMenuItems();
			menu.AddLoadingMenuItem();
			await _ReloadAvailableJobs();
		}
		IEnumerable<DeliveryJob> enumerable = deliveryJobCache.Where((DeliveryJob j) => j.GetDeliveryTimeLeft() >= TimeSpan.FromHours(5.0));
		switch (deliveriesMenuSortMode)
		{
		case DeliveriesMenuSortMode.Payout:
			menu.MenuSubtitle = LocalizationController.S(Entries.Jobs.MENU_DDRIVER_DELIVERIES_SORTED_BY_PAYOUT);
			enumerable = enumerable.OrderByDescending((DeliveryJob j) => j.PaymentAmount);
			break;
		case DeliveriesMenuSortMode.PickupDistance:
			menu.MenuSubtitle = LocalizationController.S(Entries.Jobs.MENU_DDRIVER_DELIVERIES_SORTED_BY_DISTANCE);
			enumerable = enumerable.OrderBy(delegate(DeliveryJob j)
			{
				//IL_0005: Unknown result type (might be due to invalid IL or missing references)
				//IL_0014: Unknown result type (might be due to invalid IL or missing references)
				//IL_0023: Unknown result type (might be due to invalid IL or missing references)
				//IL_0033: Unknown result type (might be due to invalid IL or missing references)
				//IL_0043: Unknown result type (might be due to invalid IL or missing references)
				//IL_0053: Unknown result type (might be due to invalid IL or missing references)
				//IL_0071: Unknown result type (might be due to invalid IL or missing references)
				//IL_0076: Unknown result type (might be due to invalid IL or missing references)
				//IL_007b: Unknown result type (might be due to invalid IL or missing references)
				//IL_0083: Unknown result type (might be due to invalid IL or missing references)
				float num = API.CalculateTravelDistanceBetweenPoints(((Entity)Game.PlayerPed).Position.X, ((Entity)Game.PlayerPed).Position.Y, ((Entity)Game.PlayerPed).Position.Z, j.PickUpLocation.Coordinates.X, j.PickUpLocation.Coordinates.Y, j.PickUpLocation.Coordinates.Z);
				if (num == 100000f)
				{
					Vector3 val = j.PickUpLocation.Coordinates.XYZ();
					num = (float)Math.Sqrt(((Vector3)(ref val)).DistanceToSquared2D(((Entity)Game.PlayerPed).Position));
				}
				return num;
			});
			break;
		}
		menu.ClearMenuItems();
		menu.CounterPreText = $"{enumerable.Count()} jobs";
		if (enumerable.Count() == 0)
		{
			menu.AddMenuItem(new MenuItem("No delivery jobs :(", "There are currently no available delivery jobs.")
			{
				Enabled = false
			});
			return;
		}
		foreach (DeliveryJob item in enumerable)
		{
			menu.AddMenuItem(item.ToMenuItem());
		}
	}

	public static async void RefreshDeliveriesMenu(bool forceReload = true)
	{
		instance._RefreshDeliveriesMenu(forceReload);
	}

	private async void _RefreshHistoryMenu()
	{
		Menu menu = subMenus["history"];
		menu.ClearMenuItems();
		menu.CounterPreText = $"{pastDeliveries.Count()} jobs";
		if (pastDeliveries.Count == 0)
		{
			menu.AddMenuItem(new MenuItem("No past deliveries :(", "There are no past deliveries to show.")
			{
				Enabled = false
			});
			return;
		}
		foreach (PastDeliveryInfo pastDelivery in pastDeliveries)
		{
			menu.AddMenuItem(pastDelivery.ToMenuItem());
		}
	}

	public static void OpenDeliveriesMenu()
	{
		MenuController.CloseAllMenus();
		subMenus["deliveries"].OpenMenu();
	}

	private async void OnDeliveriesMenuSort(Menu menu, Control control)
	{
		deliveriesMenuSortMode++;
		if (deliveriesMenuSortMode == (DeliveriesMenuSortMode)Enum.GetValues(typeof(DeliveriesMenuSortMode)).Length)
		{
			deliveriesMenuSortMode = DeliveriesMenuSortMode.Payout;
		}
		_RefreshDeliveriesMenu(forceReload: false);
	}

	private async void OnDeliveriesMenuRefresh(Menu menu, Control control)
	{
		if (!Gtacnr.Utils.CheckTimePassed(manualRefreshTimestamp, 5000.0))
		{
			Utils.SendNotification(LocalizationController.S(Entries.Jobs.DDRIVER_JOB_MENU_REFRESH_COOLDOWN));
			Utils.PlayErrorSound();
		}
		else
		{
			manualRefreshTimestamp = DateTime.UtcNow;
			_RefreshDeliveriesMenu();
		}
	}

	[EventHandler("gtacnr:time")]
	private void OnTime(int hour, int minute, int dayOfWeek)
	{
		Menu currentMenu = MenuController.GetCurrentMenu();
		if (currentMenu == subMenus["deliveries"])
		{
			MenuItem currentMenuItem = currentMenu.GetCurrentMenuItem();
			RefreshMenuItemCountdown(currentMenuItem);
		}
	}

	private void RefreshMenuItemCountdown(MenuItem menuItem)
	{
		if (menuItem != null && menuItem.ItemData is DeliveryJob deliveryJob)
		{
			TimeSpan deliveryTimeLeft = deliveryJob.GetDeliveryTimeLeft();
			string arg = ((deliveryTimeLeft.TotalHours > 15.0) ? "~g~" : ((deliveryTimeLeft.TotalHours > 8.0) ? "~y~" : "~r~"));
			string text = $"{arg}{Math.Floor(deliveryTimeLeft.TotalHours):00}:{deliveryTimeLeft.Minutes:00}";
			menuItem.Description = menuItem.Description.ReplaceDelimitedString(text);
		}
	}
}
