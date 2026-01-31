using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using CitizenFX.Core.UI;
using Gtacnr.Client.Anticheat;
using Gtacnr.Client.API;
using Gtacnr.Client.Crimes;
using Gtacnr.Client.Estates.Garages;
using Gtacnr.Client.HUD;
using Gtacnr.Client.IMenu;
using Gtacnr.Client.Jobs;
using Gtacnr.Client.Premium;
using Gtacnr.Client.Vehicles;
using Gtacnr.Client.Vehicles.Behaviors;
using Gtacnr.Data;
using Gtacnr.Localization;
using Gtacnr.Model;
using Gtacnr.Model.Enums;
using Gtacnr.ResponseCodes;
using MenuAPI;
using NativeUI;

namespace Gtacnr.Client.Businesses.Dealerships;

public class DealershipMenuScript : Script
{
	private static DealershipMenuScript instance;

	private Menu mainMenu;

	private Menu optionsMenu;

	private Menu garagesMenu;

	private Menu giftCardsMenu;

	private Dictionary<string, Menu> categoryMenus = new Dictionary<string, Menu>();

	private Dictionary<string, MenuItem> menuItems = new Dictionary<string, MenuItem>();

	private int dealerCamera;

	private Ped dealershipCarMenuPed;

	private static HashSet<StoredVehicle> vehiclesCache;

	private Dealership currentDealership;

	private Vehicle selectedVehicle;

	private PersonalVehicleModel selectedVehicleModelData;

	private DealershipSupply selectedSupply;

	private bool isBusy;

	private bool isLoadingVehiclePreview;

	private bool isInDealershipMenu;

	private bool isInTestDrive;

	private bool leftReturnMarker;

	private bool farHelpTextShown;

	private float currentSpeed;

	private SpeedMeasurementUnit measureUnit;

	private const int testDriveSeconds = 120;

	private int secondsLeftForTestDrive = 120;

	private TextTimerBar testDriveTimerBar;

	private string currentVehicleFullName;

	private Job jobData;

	private string vanityPlate;

	public DealershipMenuScript()
	{
		instance = this;
	}

	protected override void OnStarted()
	{
		mainMenu = new Menu(LocalizationController.S(Entries.Businesses.MENU_DEALERSHIP_TITLE), LocalizationController.S(Entries.Businesses.MENU_DEALERSHIP_TITLE));
		mainMenu.OnMenuClose += OnMenuClose;
		mainMenu.OnIndexChange += OnMenuIndexChange;
		optionsMenu = new Menu(LocalizationController.S(Entries.Businesses.MENU_DEALERSHIP_TITLE), LocalizationController.S(Entries.Businesses.MENU_DEALERSHIP_OPTIONS_SUBTITLE));
		optionsMenu.OnMenuOpen += OnMenuOpen;
		optionsMenu.OnItemSelect += OnMenuItemSelect;
		optionsMenu.OnListIndexChange += OnMenuListIndexChange;
		garagesMenu = new Menu(LocalizationController.S(Entries.Businesses.MENU_DEALERSHIP_GARAGES_TITLE), LocalizationController.S(Entries.Businesses.MENU_DEALERSHIP_GARAGES_SUBTITLE));
		garagesMenu.OnItemSelect += OnMenuItemSelect;
	}

	private async Coroutine CheckMenuOpenTask()
	{
		await Script.Wait(1000);
		if (isInDealershipMenu && !isInTestDrive && !Utils.IsScreenFadingInProgress() && !API.IsScreenFadedOut() && !MenuController.IsAnyMenuOpen() && currentDealership != null)
		{
			await OpenMenuInternal(currentDealership.ParentBusiness);
		}
	}

	private async Coroutine PreventMovementTask()
	{
		if (isInDealershipMenu && !isInTestDrive)
		{
			API.DisableAllControlActions(0);
		}
	}

	private void SetIsInDealershipMenu(bool toggle)
	{
		if (toggle && !isInDealershipMenu)
		{
			isInDealershipMenu = true;
			AttachDealershipMenuTasks();
			API.DisplayRadar(false);
		}
		else if (!toggle && isInDealershipMenu)
		{
			isInDealershipMenu = false;
			DetachDealershipMenuTasks();
			API.DisplayRadar(true);
		}
	}

	private void AttachDealershipMenuTasks()
	{
		base.Update += CheckMenuOpenTask;
		base.Update += PreventMovementTask;
	}

	private void DetachDealershipMenuTasks()
	{
		base.Update -= CheckMenuOpenTask;
		base.Update -= PreventMovementTask;
	}

	private void SetIsInTestDrive(bool toggle)
	{
		if (toggle && !isInTestDrive)
		{
			isInTestDrive = true;
			farHelpTextShown = false;
			leftReturnMarker = false;
			currentVehicleFullName = Utils.GetVehicleFullName(selectedVehicleModelData.Id);
			AttachTestDriveTasks();
		}
		else if (!toggle && isInTestDrive)
		{
			isInTestDrive = false;
			DetachTestDriveTasks();
		}
	}

	private void AttachTestDriveTasks()
	{
		base.Update += LeaveTestDriveTask;
		base.Update += UpdateSpeedometerTask;
		base.Update += DrawTestDriveTask;
	}

	private void DetachTestDriveTasks()
	{
		base.Update -= LeaveTestDriveTask;
		base.Update -= UpdateSpeedometerTask;
		base.Update -= DrawTestDriveTask;
	}

	private async Coroutine LeaveTestDriveTask()
	{
		await Script.Wait(1000);
		if (!isInTestDrive)
		{
			return;
		}
		Vector3 position = ((Entity)Game.PlayerPed).Position;
		float num = ((Vector3)(ref position)).DistanceToSquared(currentDealership.CarOutPosition.XYZ());
		if (!leftReturnMarker && num > 10f)
		{
			leftReturnMarker = true;
		}
		else if (leftReturnMarker && num <= 5f)
		{
			ExitTestDrive(0);
			return;
		}
		secondsLeftForTestDrive--;
		UpdateTestDriveTimer();
		if (secondsLeftForTestDrive <= 0)
		{
			ExitTestDrive(1);
			return;
		}
		if ((Entity)(object)Game.PlayerPed.CurrentVehicle != (Entity)(object)selectedVehicle)
		{
			ExitTestDrive(2);
			return;
		}
		if (num > 562500f && !farHelpTextShown)
		{
			farHelpTextShown = true;
			Utils.DisplayHelpText(LocalizationController.S(Entries.Jobs.DEALERSHIP_GETTING_TOO_FAR_AWAY));
		}
		if (num > 1000000f)
		{
			ExitTestDrive(3);
		}
		else if (LatentPlayers.Get(Game.Player.ServerId).WantedLevel > 1)
		{
			ExitTestDrive(4);
		}
		else if (selectedVehicle.BodyHealth < 998f)
		{
			ExitTestDrive(5);
		}
	}

	private async Coroutine UpdateSpeedometerTask()
	{
		await Script.Wait(50);
		API.DisplayRadar(false);
		if ((Entity)(object)Game.PlayerPed.CurrentVehicle == (Entity)(object)selectedVehicle)
		{
			measureUnit = ((API.GetProfileSetting(227) == 1) ? SpeedMeasurementUnit.Kmh : SpeedMeasurementUnit.Mph);
			switch (measureUnit)
			{
			case SpeedMeasurementUnit.Mph:
				currentSpeed = selectedVehicle.Speed.ToMph();
				break;
			case SpeedMeasurementUnit.Kmh:
				currentSpeed = selectedVehicle.Speed.ToKmh();
				break;
			}
		}
	}

	private async Coroutine DrawTestDriveTask()
	{
		if (!isInTestDrive)
		{
			return;
		}
		if (leftReturnMarker)
		{
			Vector3 val = currentDealership.CarOutPosition.XYZ();
			Vector3 val2 = default(Vector3);
			((Vector3)(ref val2))._002Ector(3.5f, 3.5f, 0.75f);
			Color color = Color.FromUint(3137339520u);
			float z = 0f;
			if (API.GetGroundZFor_3dCoord(val.X, val.Y, val.Z, ref z, false))
			{
				val.Z = z;
			}
			API.DrawMarker(1, val.X, val.Y, val.Z, 0f, 0f, 0f, 0f, 0f, 0f, val2.X, val2.Y, val2.Z, (int)color.R, (int)color.G, (int)color.B, (int)color.A, false, true, 2, false, (string)null, (string)null, false);
		}
		if ((Entity)(object)selectedVehicle != (Entity)null)
		{
			string description = Gtacnr.Utils.GetDescription(measureUnit);
			Utils.Draw2DText($"{currentSpeed:0} {description.ToUpperInvariant()}", new Vector2(0.5f, 0.9f), new Color(220, 220, 220, 220), 1.4f, 4, (Alignment)0, drawOutline: true, new Color(0, 0, 0, byte.MaxValue), 5);
			Utils.Draw2DText(currentVehicleFullName ?? "", new Vector2(0.01f, 0.94f), new Color(220, 220, 220, 220), 0.8f, 1, (Alignment)1, drawOutline: true, new Color(0, 0, 0, byte.MaxValue), 5);
		}
	}

	private void ShowTestDriveTimer()
	{
		secondsLeftForTestDrive = 120;
		string text = Gtacnr.Utils.SecondsToMinutesAndSeconds(secondsLeftForTestDrive);
		testDriveTimerBar = new TextTimerBar("TIME LEFT", text);
		TimerBarScript.AddTimerBar(testDriveTimerBar);
	}

	private void HideTestDriveTimer()
	{
		TimerBarScript.RemoveTimerBar(testDriveTimerBar);
		testDriveTimerBar = null;
	}

	private void UpdateTestDriveTimer()
	{
		if (testDriveTimerBar != null)
		{
			testDriveTimerBar.Text = Gtacnr.Utils.SecondsToMinutesAndSeconds(secondsLeftForTestDrive);
		}
		if (secondsLeftForTestDrive < 20)
		{
			testDriveTimerBar.TextColor = Colors.GTARed;
		}
		else
		{
			testDriveTimerBar.TextColor = Colors.White;
		}
	}

	private async void EnterTestDrive()
	{
		if (isInTestDrive)
		{
			Utils.PlayErrorSound();
			return;
		}
		SetIsInTestDrive(toggle: true);
		MenuController.CloseAllMenus();
		await Utils.FadeOut();
		((Entity)selectedVehicle).PositionNoOffset = currentDealership.CarOutPosition.XYZ();
		((Entity)selectedVehicle).Heading = currentDealership.CarOutPosition.W;
		StopDealershipView(killPed: false);
		Game.PlayerPed.Task.ClearAllImmediately();
		Game.PlayerPed.Task.WarpIntoVehicle(selectedVehicle, (VehicleSeat)(-1));
		dealershipCarMenuPed.Task.ClearAllImmediately();
		dealershipCarMenuPed.Task.WarpIntoVehicle(selectedVehicle, (VehicleSeat)0);
		selectedVehicle.Repair();
		DisableMountedGunsScript.DisableMountedGuns(selectedVehicle);
		API.SetGameplayCamRelativePitch(0f, 1f);
		API.SetGameplayCamRelativeHeading(0f);
		ShowTestDriveTimer();
		await Utils.FadeIn();
	}

	private async void ExitTestDrive(int reason)
	{
		if (!isInTestDrive)
		{
			Utils.PlayErrorSound();
			return;
		}
		SetIsInTestDrive(toggle: false);
		await Utils.FadeOut();
		HideTestDriveTimer();
		if (reason == 4)
		{
			BaseScript.TriggerServerEvent("gtacnr:dealership:loseTestDriveMoney", new object[0]);
			await ExitDealership();
			Utils.DisplaySubtitle(LocalizationController.S(Entries.Jobs.DEALERSHIP_DRIVE_WANTED_LEVEL));
			return;
		}
		await SetupDealershipView();
		await SetSelectedVehicle(selectedVehicleModelData);
		await Utils.FadeIn();
		mainMenu.OpenMenu();
		await BaseScript.Delay(500);
		bool flag = false;
		switch (reason)
		{
		case 0:
			flag = true;
			break;
		case 1:
			Utils.DisplaySubtitle(LocalizationController.S(Entries.Jobs.DEALERSHIP_DRIVE_TIME));
			flag = true;
			break;
		case 2:
			Utils.DisplaySubtitle(LocalizationController.S(Entries.Jobs.DEALERSHIP_DRIVE_LEFT));
			flag = true;
			break;
		case 3:
			Utils.DisplaySubtitle(LocalizationController.S(Entries.Jobs.DEALERSHIP_DRIVE_TOO_FAR_AWAY));
			flag = true;
			break;
		case 5:
			Utils.DisplaySubtitle(LocalizationController.S(Entries.Jobs.DEALERSHIP_DRIVE_DAMAGED));
			break;
		}
		if (flag)
		{
			BaseScript.TriggerServerEvent("gtacnr:dealership:returnTestDriveMoney", new object[0]);
		}
		else
		{
			BaseScript.TriggerServerEvent("gtacnr:dealership:loseTestDriveMoney", new object[0]);
		}
	}

	public static async void OpenDealershipMenu(Business business)
	{
		await instance.OpenMenuInternal(business);
	}

	public static async void OpenMenu(Dealership dealership)
	{
		await instance.OpenMenuInternal(dealership.ParentBusiness);
	}

	private async Task OpenMenuInternal(Business business)
	{
		if (isBusy || business.Dealership == null)
		{
			Utils.PlayErrorSound();
			return;
		}
		currentDealership = business.Dealership;
		if (currentDealership == null)
		{
			Utils.PlayErrorSound();
			return;
		}
		string cachedJob = Gtacnr.Client.API.Jobs.CachedJob;
		jobData = Gtacnr.Data.Jobs.GetJobData(cachedJob);
		bool flag = false;
		if (currentDealership.AllowedJobs != null)
		{
			if (!currentDealership.AllowedJobs.Contains(cachedJob))
			{
				Utils.DisplayHelpText(LocalizationController.S(Entries.Jobs.DEALERSHIP_NOT_ACCESSIBLE_WHEN_JOB, jobData.GetColoredName()));
				return;
			}
			flag = true;
		}
		if (jobData.SeparateVehicles && !flag)
		{
			Utils.DisplayHelpText(LocalizationController.S(Entries.Jobs.DEALERSHIP_CANT_ENTER_WHEN_JOB, jobData.GetColoredName()));
			return;
		}
		if (await Gtacnr.Client.API.Crime.GetWantedLevel() > 1)
		{
			BusinessEmployee businessEmployee = business.Employees.Where((BusinessEmployee e) => e.Role == EmployeeRole.Cashier).FirstOrDefault();
			if (businessEmployee != null)
			{
				BusinessEmployeeState state = businessEmployee.State;
				if (state != null)
				{
					Ped ped = state.Ped;
					if (ped != null)
					{
						ped.PlayAmbientSpeech("GENERIC_INSULT_MED", (SpeechModifier)3);
					}
				}
			}
			Utils.DisplayHelpText(LocalizationController.S(Entries.Player.WANTED_BY_POLICE), playSound: false);
			Utils.PlayErrorSound();
			return;
		}
		bool success = false;
		isBusy = true;
		try
		{
			await Utils.FadeOut(500);
			EnterDealershipResponse enterDealershipResponse = (EnterDealershipResponse)(await TriggerServerEventAsync<int>("gtacnr:dealership:enterDealership", new object[1] { business.Id }));
			if (enterDealershipResponse != EnterDealershipResponse.Success)
			{
				Utils.DisplayHelpText(LocalizationController.S(Entries.Main.UNEXPECTED_ERROR_CODE, $"0x6E-{(int)enterDealershipResponse}"));
				return;
			}
			DealershipScript.IsInDealership = true;
			PickpocketScript.Instance.CancelWalletTheftMission();
			await RefreshMainMenu();
			Menu value = categoryMenus.FirstOrDefault().Value;
			if (value == null)
			{
				Utils.DisplayHelpText(LocalizationController.S(Entries.Main.UNEXPECTED_ERROR_CODE, "0x6E-B"));
				return;
			}
			MenuItem menuItem = value.GetMenuItems().FirstOrDefault();
			if (menuItem == null || !(menuItem.ItemData is DealershipSupply dealershipSupply))
			{
				Utils.DisplayHelpText(LocalizationController.S(Entries.Main.UNEXPECTED_ERROR_CODE, "0x6E-C"));
				return;
			}
			PersonalVehicleModel firstVehicleData = DealershipScript.VehicleModelData[dealershipSupply.Vehicle];
			if (firstVehicleData == null)
			{
				Utils.DisplayHelpText(LocalizationController.S(Entries.Main.UNEXPECTED_ERROR_CODE, "0x6E-D"));
				return;
			}
			selectedSupply = dealershipSupply;
			Vehicle[] allVehicles = World.GetAllVehicles();
			for (int num = 0; num < allVehicles.Length; num++)
			{
				((PoolObject)allVehicles[num]).Delete();
			}
			foreach (Ped item in from p in World.GetAllPeds()
				where !p.IsPlayer
				select p)
			{
				((PoolObject)item).Delete();
			}
			if (!(await SetupDealershipView()))
			{
				Utils.DisplayHelpText(LocalizationController.S(Entries.Main.UNEXPECTED_ERROR_CODE, "0x6E-E"));
				return;
			}
			await SetSelectedVehicle(firstVehicleData);
			await Utils.FadeIn(500);
			mainMenu.OpenMenu();
			SetIsInDealershipMenu(toggle: true);
			success = true;
		}
		catch (Exception exception)
		{
			Print(exception);
			Utils.DisplayHelpText(LocalizationController.S(Entries.Main.UNEXPECTED_ERROR_CODE, "0x6E-F"));
			DealershipScript.IsInDealership = false;
		}
		finally
		{
			Finally();
		}
		async void Finally()
		{
			if (API.IsScreenFadedOut())
			{
				await Utils.FadeIn(500);
			}
			isBusy = false;
			if (!success)
			{
				StopDealershipView();
				SetIsInDealershipMenu(toggle: false);
				MenuController.CloseAllMenus();
				DealershipScript.IsInDealership = false;
				BaseScript.TriggerServerEvent("gtacnr:dealership:enterDealershipFailed", new object[0]);
			}
		}
	}

	private async void OnMenuOpen(Menu menu)
	{
		if (menu == optionsMenu)
		{
			await RefreshOptionsMenu();
		}
		else if (categoryMenus.Any<KeyValuePair<string, Menu>>((KeyValuePair<string, Menu> c) => c.Value == menu))
		{
			selectedSupply = menu.GetCurrentMenuItem().ItemData as DealershipSupply;
		}
	}

	private async void OnMenuClose(Menu menu, MenuClosedEventArgs e)
	{
		if (menu == mainMenu && e.ClosedByUser && !e.IsOpeningSubmenu && !isInTestDrive)
		{
			await BaseScript.Delay(1);
			await ExitDealership();
		}
	}

	private async Task<bool> ExitDealership(bool spawnAtOutPos = false, bool fadeIn = true)
	{
		SetIsInDealershipMenu(toggle: false);
		isBusy = true;
		try
		{
			await Utils.FadeOut();
			if (!(await instance.TriggerServerEventAsync<bool>("gtacnr:dealership:exitDealership", new object[0])))
			{
				Utils.DisplayHelpText(LocalizationController.S(Entries.Main.UNEXPECTED_ERROR_CODE, "0x6F"));
				await Utils.FadeIn();
				mainMenu.OpenMenu();
				SetIsInDealershipMenu(toggle: true);
				return false;
			}
			if ((Entity)(object)selectedVehicle != (Entity)null && selectedVehicle.Exists())
			{
				((PoolObject)selectedVehicle).Delete();
			}
			if (spawnAtOutPos)
			{
				await Utils.TeleportToCoords(currentDealership.CarOutPosition);
			}
			StopDealershipView();
			if (fadeIn)
			{
				await Utils.FadeIn();
			}
			DealershipScript.IsInDealership = false;
			Game.PlayerPed.Task.ClearAll();
			MenuController.CloseAllMenus();
			return true;
		}
		catch (Exception exception)
		{
			Print(exception);
			mainMenu.OpenMenu();
			SetIsInDealershipMenu(toggle: true);
			return false;
		}
		finally
		{
			isBusy = false;
		}
	}

	private async void OnMenuItemSelect(Menu menu, MenuItem menuItem, int itemIndex)
	{
		if (menu == optionsMenu)
		{
			if (IsSelected("testDrive"))
			{
				if (!isBusy)
				{
					try
					{
						isBusy = true;
						int num = await TriggerServerEventAsync<int>("gtacnr:dealership:depositTestDriveMoney", new object[1] { selectedSupply.Vehicle });
						switch (num)
						{
						case 1:
							EnterTestDrive();
							break;
						case 3:
							Utils.DisplayHelpText(LocalizationController.S(Entries.Main.NOT_ENOUGH_MONEY));
							break;
						default:
							Utils.DisplayErrorMessage(172, num);
							break;
						}
						return;
					}
					catch (Exception exception)
					{
						Print(exception);
						return;
					}
					finally
					{
						isBusy = false;
					}
				}
				Utils.PlayErrorSound();
			}
			else if (IsSelected("purchase"))
			{
				if (jobData.SeparateVehicles || currentDealership.IsRental)
				{
					await BuyVehicle(null);
				}
				else if (GarageScript.OwnedGaragesCount() == 0)
				{
					Utils.DisplayHelpText(LocalizationController.S(Entries.Jobs.DEALERSHIP_NEED_GARAGE_FIRST), playSound: false);
					Utils.PlayErrorSound();
				}
				else
				{
					OpenGaragesMenu();
				}
			}
			else
			{
				if (!IsSelected("plate"))
				{
					return;
				}
				string text = await Utils.GetUserInput(LocalizationController.S(Entries.Jobs.MECHANIC_PLATE_INPUT_TITLE), LocalizationController.S(Entries.Jobs.MECHANIC_PLATE_INPUT_TEXT), "", 8);
				if (text == null)
				{
					vanityPlate = null;
					Utils.SendNotification(LocalizationController.S(Entries.Imenu.IMENU_DEALERSHIP_PLATE_CLEARED));
					return;
				}
				string value = await ValidateVanityPlate(text);
				if (!string.IsNullOrEmpty(value))
				{
					vanityPlate = value;
					menuItems["plate"].Label = vanityPlate;
					Utils.SendNotification(LocalizationController.S(Entries.Imenu.IMENU_DEALERSHIP_PLATE_CHANGED_SUCCESSFULLY, vanityPlate));
				}
			}
		}
		else if (menu == garagesMenu)
		{
			await BuyVehicle(menuItem.ItemData as Garage);
		}
		else if (menu == giftCardsMenu && menuItem.ItemData is InventoryEntry inventoryEntry && (Gtacnr.Data.Items.GetItemDefinition(inventoryEntry.ItemId).ExtraData?.ContainsKey("GiftCardType") ?? false))
		{
			await RedeemGiftCard(inventoryEntry, menuItem);
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

	private async Task RedeemGiftCard(InventoryEntry entry, MenuItem menuItem)
	{
		if (entry.Amount < 1f)
		{
			Utils.PlayErrorSound();
		}
		else if (await TriggerServerEventAsync<int>("gtacnr:dealership:redeemGiftCard", new object[2]
		{
			currentDealership.ParentBusiness.Id,
			entry.ItemId
		}) == 1)
		{
			Utils.PlayContinueSound();
			if (entry.Amount == 0f)
			{
				menuItem.ParentMenu.RemoveMenuItem(menuItem);
			}
			else
			{
				menuItem.Label = $"{entry.Amount:0}";
			}
			await UpdateGiftCardBalance();
		}
		else
		{
			Utils.PlayErrorSound();
		}
	}

	private async Task<long> UpdateGiftCardBalance()
	{
		mainMenu.CounterPreText = "";
		long num = await Money.GetCachedBalanceOrFetch(AccountType.DealershipGiftCard);
		if (num != 0L)
		{
			mainMenu.CounterPreText = LocalizationController.S(Entries.Businesses.MENU_STORE_GIFT_CARD_BALANCE, num.ToCurrencyString());
		}
		return num;
	}

	private async Task BuyVehicle(Garage garage)
	{
		if (isBusy)
		{
			Utils.PlayErrorSound();
			return;
		}
		try
		{
			isBusy = true;
			if (garage != null)
			{
				foreach (MenuItem menuItem in garagesMenu.GetMenuItems())
				{
					menuItem.Enabled = false;
				}
			}
			VehicleModData modData = new VehicleModData
			{
				PrimaryColor = (int)selectedVehicle.Mods.PrimaryColor,
				SecondaryColor = (int)selectedVehicle.Mods.SecondaryColor,
				TrimColor = (int)selectedVehicle.Mods.TrimColor,
				DashboardColor = (int)selectedVehicle.Mods.DashboardColor,
				Livery = selectedVehicle.Mods.Livery
			};
			long giftCardBalance = await Money.GetCachedBalanceOrFetch(AccountType.DealershipGiftCard);
			string text = await TriggerServerEventAsync<string>("gtacnr:dealership:buyVehicle", new object[4]
			{
				selectedSupply.Vehicle,
				garage?.Id,
				vanityPlate,
				modData.Json()
			});
			if (string.IsNullOrEmpty(text))
			{
				Utils.DisplayErrorMessage(157);
				Print("Received an empty response from the server.");
				Print("Request details: gtacnr:dealership:buyVehicle (" + selectedSupply.Vehicle + ", " + garage?.Id + ", " + vanityPlate + ", " + modData.Json() + ")");
				Utils.PlayErrorSound();
				return;
			}
			BuyVehicleResponseData response = text.Unjson<BuyVehicleResponseData>();
			switch (response.Code)
			{
			case BuyItemResponse.Success:
			{
				VehiclesMenuScript.InvalidateCache();
				MenuController.CloseAllMenus();
				string verb = (currentDealership.IsRental ? "rented" : "purchased");
				string vehicleFullName = Utils.GetVehicleFullName(selectedSupply.Vehicle);
				string errMsg = "You ~g~successfully ~s~purchased a ~b~" + vehicleFullName + "~s~, however an ~r~error ~s~has occurred and your vehicle has been transferred to ~b~your garage~s~.";
				long num = selectedSupply.CalculateFinalPrice();
				long num2 = num;
				long num3 = 0L;
				if (giftCardBalance > 0)
				{
					num3 = Math.Min(num, giftCardBalance);
					num2 -= num3;
					UpdateGiftCardBalance();
				}
				if (num3 == 0L)
				{
					Utils.SendNotification("You " + verb + " a ~p~" + vehicleFullName + " ~s~for ~r~" + num2.ToCurrencyString() + "~s~.");
				}
				else
				{
					Utils.SendNotification("You " + verb + " a ~b~" + vehicleFullName + " ~s~for ~b~" + num3.ToCurrencyString() + " ~s~in gift card balance and ~r~" + num2.ToCurrencyString() + " ~s~by check.");
				}
				if (await ExitDealership(spawnAtOutPos: true, fadeIn: false))
				{
					StoredVehicle storedVehicle = response.StoredVehicle;
					Vehicle val = await Utils.CreateStoredVehicle(storedVehicle, ((Entity)Game.PlayerPed).Position, ((Entity)Game.PlayerPed).Heading);
					if ((Entity)(object)val != (Entity)null)
					{
						Game.PlayerPed.Task.WarpIntoVehicle(val, (VehicleSeat)(-1));
						await AntiEntitySpawnScript.RegisterEntity((Entity)(object)val);
						BaseScript.TriggerServerEvent("gtacnr:vehicles:registerCreatedVehicle", new object[1] { storedVehicle.Json() });
						await ActiveVehicleScript.SetActiveVehicle(storedVehicle);
						await Utils.FadeIn();
					}
					else
					{
						Utils.DisplayHelpText(errMsg);
					}
				}
				else
				{
					await BaseScript.Delay(2000);
					Utils.DisplayHelpText(errMsg);
				}
				await BaseScript.Delay(5000);
				Utils.DisplayHelpText("Manage your " + verb + " vehicles in " + MainMenuScript.OpenMenuControlString + " ~y~Menu ~s~> ~y~Vehicles");
				break;
			}
			case BuyItemResponse.NoMoney:
				Utils.DisplayHelpText(LocalizationController.S(Entries.Main.NOT_ENOUGH_MONEY));
				break;
			case BuyItemResponse.InsufficientMembershipTier:
				Utils.DisplayHelpText("~p~You don't have the required membership tier.");
				break;
			case BuyItemResponse.InsufficientMembershipTierStorage:
				Utils.DisplayHelpText("~p~You don't have the required membership tier to use this garage. Please, select another garage.");
				break;
			case BuyItemResponse.InsufficientLevel:
				Utils.DisplayHelpText("~b~You don't have the required level.");
				break;
			case BuyItemResponse.DuplicateIdentifier:
				Utils.DisplayHelpText("~r~The specified license plate is already taken.");
				break;
			case BuyItemResponse.NoSpaceLeft:
				Utils.DisplayHelpText("~r~Your garage is full. Please, select another garage.");
				break;
			default:
				Utils.DisplayErrorMessage(157, (int)response.Code);
				break;
			}
			if (garage == null)
			{
				return;
			}
			foreach (MenuItem menuItem2 in garagesMenu.GetMenuItems())
			{
				menuItem2.Enabled = true;
			}
		}
		catch (Exception exception)
		{
			Print(exception);
			Utils.DisplayErrorMessage(73, 0, LocalizationController.S(Entries.Main.UNEXPECTED_ERROR_PRESS_F8));
		}
		finally
		{
			isBusy = false;
		}
	}

	private int GetDepositValue()
	{
		return Convert.ToInt32(Math.Round((double)selectedSupply.CalculateFinalPrice() * 0.002)).Clamp(1000, 50000);
	}

	private async void OnMenuIndexChange(Menu menu, MenuItem oldItem, MenuItem newItem, int oldIndex, int newIndex)
	{
		if (currentDealership == null || !DealershipScript.IsInDealership)
		{
			return;
		}
		if (menu == mainMenu)
		{
			if (!(newItem.ItemData is Menu menu2))
			{
				return;
			}
			DealershipSupply dealershipSupply = menu2.GetMenuItems().FirstOrDefault()?.ItemData as DealershipSupply;
			PersonalVehicleModel personalVehicleModel = DealershipScript.VehicleModelData[dealershipSupply.Vehicle];
			if (personalVehicleModel == null)
			{
				return;
			}
			foreach (Menu value in categoryMenus.Values)
			{
				value.RefreshIndex();
			}
			selectedSupply = dealershipSupply;
			await SetSelectedVehicle(personalVehicleModel);
		}
		else if (menu.GetCurrentMenuItem().ItemData is DealershipSupply dealershipSupply2)
		{
			PersonalVehicleModel personalVehicleModel2 = DealershipScript.VehicleModelData[dealershipSupply2.Vehicle];
			if (personalVehicleModel2 != null)
			{
				selectedSupply = dealershipSupply2;
				await SetSelectedVehicle(personalVehicleModel2);
			}
		}
	}

	private void OnMenuListIndexChange(Menu menu, MenuListItem listItem, int oldSelectionIndex, int newSelectionIndex, int itemIndex)
	{
		//IL_00cc: Unknown result type (might be due to invalid IL or missing references)
		PersonalVehicleModel personalVehicleModel = selectedVehicleModelData;
		DealershipSupply dealershipSupply = selectedSupply;
		if (currentDealership == null || personalVehicleModel == null || dealershipSupply == null || (Entity)(object)selectedVehicle == (Entity)null)
		{
			return;
		}
		VehicleColorSet vehicleColorSet = ((dealershipSupply.Colors != null) ? dealershipSupply.Colors : personalVehicleModel.Colors);
		List<int> list = ((dealershipSupply.Liveries != null) ? dealershipSupply.Liveries : personalVehicleModel.Liveries);
		if (menu != optionsMenu)
		{
			return;
		}
		if (IsSelected("color1"))
		{
			List<int> primary = vehicleColorSet.Primary;
			selectedVehicle.Mods.PrimaryColor = (VehicleColor)primary[newSelectionIndex];
			if (vehicleColorSet.Secondary.Count == 0)
			{
				selectedVehicle.Mods.SecondaryColor = selectedVehicle.Mods.PrimaryColor;
			}
		}
		else if (IsSelected("color2"))
		{
			List<int> secondary = vehicleColorSet.Secondary;
			selectedVehicle.Mods.SecondaryColor = (VehicleColor)secondary[newSelectionIndex];
		}
		else if (IsSelected("color3"))
		{
			List<int> trim = vehicleColorSet.Trim;
			selectedVehicle.Mods.TrimColor = (VehicleColor)trim[newSelectionIndex];
		}
		else if (IsSelected("color4"))
		{
			List<int> dashboard = vehicleColorSet.Dashboard;
			selectedVehicle.Mods.DashboardColor = (VehicleColor)dashboard[newSelectionIndex];
		}
		else if (IsSelected("livery"))
		{
			selectedVehicle.Mods.Livery = list[newSelectionIndex];
		}
		bool IsSelected(string itemId)
		{
			if (menuItems.ContainsKey(itemId))
			{
				return listItem == menuItems[itemId];
			}
			return false;
		}
	}

	private async Task RefreshOptionsMenu()
	{
		Dealership dealer = currentDealership;
		PersonalVehicleModel data = selectedVehicleModelData;
		DealershipSupply supply = selectedSupply;
		MembershipTier playerTier = MembershipScript.GetCurrentMembershipTier();
		int levelByXP = Gtacnr.Utils.GetLevelByXP(await Users.GetXP());
		if (dealer == null || data == null || supply == null)
		{
			return;
		}
		string vehicleFullName = Utils.GetVehicleFullName(data.Id);
		optionsMenu.MenuTitle = (dealer.IsRental ? LocalizationController.S(Entries.Businesses.MENU_DEALERSHIP_RENTAL_TITLE) : LocalizationController.S(Entries.Businesses.MENU_DEALERSHIP_TITLE));
		optionsMenu.MenuSubtitle = vehicleFullName;
		optionsMenu.ClearMenuItems();
		if (dealer.IsRental)
		{
			optionsMenu.AddMenuItem(new MenuItem("Rental Info", "The duration of the rental is one ~r~game week~s~, which is exactly ~y~168 real minutes ~s~in real time. After it expires, you can extend it from the Vehicles menu or send the vehicle back to the rental company. You cannot modify or respray rented vehicles, nor change their license plate."));
		}
		VehicleColorSet vehicleColorSet = ((supply.Colors != null) ? supply.Colors : data.Colors);
		List<int> list = ((supply.Liveries != null) ? supply.Liveries : data.Liveries);
		MenuItem item;
		if (vehicleColorSet.Primary.Count > 1)
		{
			List<string> items = vehicleColorSet.Primary.Select((int c) => DealershipScript.VehicleColors[c].Description).ToList();
			int num = vehicleColorSet.Primary.IndexOf((int)selectedVehicle.Mods.PrimaryColor);
			if (num == -1)
			{
				num = 0;
			}
			Menu menu = optionsMenu;
			item = (menuItems["color1"] = new MenuListItem("Primary Color", items, num));
			menu.AddMenuItem(item);
		}
		if (vehicleColorSet.Secondary.Count > 1)
		{
			List<string> items2 = vehicleColorSet.Secondary.Select((int c) => DealershipScript.VehicleColors[c].Description).ToList();
			int num2 = vehicleColorSet.Secondary.IndexOf((int)selectedVehicle.Mods.SecondaryColor);
			if (num2 == -1)
			{
				num2 = 0;
			}
			Menu menu2 = optionsMenu;
			item = (menuItems["color2"] = new MenuListItem("Secondary Color", items2, num2));
			menu2.AddMenuItem(item);
		}
		if (vehicleColorSet.Trim.Count > 1)
		{
			List<string> items3 = vehicleColorSet.Trim.Select((int c) => DealershipScript.VehicleColors[c].Description).ToList();
			int num3 = vehicleColorSet.Trim.IndexOf((int)selectedVehicle.Mods.TrimColor);
			if (num3 == -1)
			{
				num3 = 0;
			}
			Menu menu3 = optionsMenu;
			item = (menuItems["color3"] = new MenuListItem("Interior Color", items3, num3));
			menu3.AddMenuItem(item);
		}
		if (vehicleColorSet.Dashboard.Count > 1)
		{
			List<string> items4 = vehicleColorSet.Dashboard.Select((int c) => DealershipScript.VehicleColors[c].Description).ToList();
			int num4 = vehicleColorSet.Dashboard.IndexOf((int)selectedVehicle.Mods.DashboardColor);
			if (num4 == -1)
			{
				num4 = 0;
			}
			Menu menu4 = optionsMenu;
			item = (menuItems["color4"] = new MenuListItem("Dashboard Color", items4, num4));
			menu4.AddMenuItem(item);
		}
		if (list.Count > 0)
		{
			List<string> items5 = list.Select((int c) => c.ToString()).ToList();
			list.IndexOf(selectedVehicle.Mods.Livery);
			_ = -1;
			Menu menu5 = optionsMenu;
			item = (menuItems["livery"] = new MenuListItem("Livery", items5));
			menu5.AddMenuItem(item);
		}
		if (!dealer.IsRental)
		{
			selectedVehicle.Mods.InstallModKit();
			VehicleMod[] allMods = selectedVehicle.Mods.GetAllMods();
			string description;
			if (allMods.Length == 0)
			{
				description = "~y~No mods available for this vehicle.";
			}
			else
			{
				description = "~b~Available mods: ~s~";
				HashSet<VehicleModType> hashSet = new HashSet<VehicleModType>();
				VehicleMod[] array = allMods;
				foreach (VehicleMod val in array)
				{
					try
					{
						if (!hashSet.Contains(val.ModType))
						{
							hashSet.Add(val.ModType);
							int modType = (int)val.ModType;
							VehicleModPricingInfo vehicleModPricingInfo = SellToPlayersScript.VehicleMods.FirstOrDefault((VehicleModPricingInfo i) => i.Id == modType);
							if (vehicleModPricingInfo != null)
							{
								string name = vehicleModPricingInfo.Name;
								description = description + name + ", ";
							}
						}
					}
					catch (Exception exception)
					{
						Print(exception);
					}
				}
				description = description.Trim().TrimEnd(',');
			}
			optionsMenu.AddMenuItem(new MenuItem("Mod Info", description));
			Menu menu6 = optionsMenu;
			item = (menuItems["plate"] = new MenuItem("Vanity License Plate"));
			menu6.AddMenuItem(item);
			vanityPlate = null;
			if ((int)playerTier >= 1)
			{
				menuItems["plate"].Description = "~p~" + LocalizationController.S(Entries.Businesses.ITEM_ATTRIBUTE_UNLOCKED_WITH_MEMBERSHIP, Gtacnr.Utils.GetDescription(MembershipTier.Silver));
			}
			else
			{
				menuItems["plate"].Enabled = false;
				menuItems["plate"].RightIcon = MenuItem.Icon.LOCK;
				menuItems["plate"].Description = "~p~" + LocalizationController.S(Entries.Businesses.ITEM_ATTRIBUTE_REQUIRES_MEMBERSHIP, Gtacnr.Utils.GetDescription(MembershipTier.Silver), ExternalLinks.Collection.Store);
			}
		}
		Menu menu7 = optionsMenu;
		item = (menuItems["testDrive"] = new MenuItem("~b~Test Drive")
		{
			Label = "~b~" + GetDepositValue().ToCurrencyString(),
			Description = "You will deposit ~b~" + GetDepositValue().ToCurrencyString() + " ~s~to pay the dealer in case of ~r~damage ~s~to the vehicle or other unfortunate events. ~y~20% ~s~of the deposit will be kept as a ~y~service fee~s~."
		});
		menu7.AddMenuItem(item);
		int amount = supply.CalculateFinalPrice();
		menuItems["purchase"] = new MenuItem(dealer.IsRental ? "~g~Rent" : "~g~Buy Now")
		{
			Label = (dealer.IsRental ? ("~g~" + amount.ToCurrencyString() + " ~c~/week") : ("~g~" + amount.ToCurrencyString())),
			Description = (dealer.IsRental ? ("Rent this ~b~" + vehicleFullName + " ~s~for ~g~" + amount.ToCurrencyString() + " ~s~a week from your bank account.") : ("Buy this ~b~" + vehicleFullName + " ~s~now for ~g~" + amount.ToCurrencyString() + " ~s~from your bank account."))
		};
		if ((int)data.MembershipTier > 0 && !dealer.IsRental)
		{
			if ((int)playerTier >= (int)data.MembershipTier)
			{
				if (playerTier == data.MembershipTier)
				{
					MenuItem menuItem8 = menuItems["purchase"];
					menuItem8.Description = menuItem8.Description + "\n~p~" + LocalizationController.S(Entries.Businesses.ITEM_ATTRIBUTE_UNLOCKED_WITH_MEMBERSHIP, Gtacnr.Utils.GetDescription(playerTier));
				}
				else
				{
					item = menuItems["purchase"];
					item.Description = item.Description + "\n~p~" + LocalizationController.S(Entries.Businesses.ITEM_ATTRIBUTE_UNLOCKED_WITH_MEMBERSHIP, Gtacnr.Utils.GetDescription(playerTier)) + " " + LocalizationController.S(Entries.Businesses.ITEM_ATTRIBUTE_UNLOCKED_WITH_MEMBERSHIP_REQUIRES_LOWER_TIER, Gtacnr.Utils.GetDescription(data.MembershipTier));
				}
			}
			else
			{
				menuItems["purchase"].Enabled = false;
				menuItems["purchase"].RightIcon = MenuItem.Icon.LOCK;
				MenuItem menuItem9 = menuItems["purchase"];
				menuItem9.Description = menuItem9.Description + "\n~p~" + LocalizationController.S(Entries.Businesses.ITEM_ATTRIBUTE_REQUIRES_MEMBERSHIP, Gtacnr.Utils.GetDescription(data.MembershipTier), ExternalLinks.Collection.Store);
			}
		}
		else if (data.RequiredLevel > 0 && !dealer.IsRental)
		{
			MenuItem menuItem10 = menuItems["purchase"];
			menuItem10.Description = menuItem10.Description + "\n~b~" + LocalizationController.S(Entries.Businesses.ITEM_ATTRIBUTE_UNLOCK_LEVEL, data.RequiredLevel);
			if (levelByXP < data.RequiredLevel)
			{
				menuItems["purchase"].Enabled = false;
				menuItems["purchase"].RightIcon = MenuItem.Icon.LOCK;
			}
		}
		if (GarageScript.OwnedGaragesCount() == 0 && !jobData.SeparateVehicles && !dealer.IsRental)
		{
			menuItems["purchase"].Description = "~r~" + LocalizationController.S(Entries.Vehicles.DEALERSHIP_NEED_TO_OWN_GARAGE);
			menuItems["purchase"].Enabled = false;
		}
		optionsMenu.AddMenuItem(menuItems["purchase"]);
	}

	private async Task RefreshMainMenu()
	{
		_ = 4;
		try
		{
			if (currentDealership == null)
			{
				return;
			}
			Dealership dealer = currentDealership;
			MembershipTier membershipTier = MembershipScript.GetCurrentMembershipTier();
			Gtacnr.Utils.GetLevelByXP(await Users.GetXP());
			mainMenu.MenuTitle = (dealer.IsRental ? LocalizationController.S(Entries.Businesses.MENU_DEALERSHIP_RENTAL_TITLE) : LocalizationController.S(Entries.Businesses.MENU_DEALERSHIP_TITLE));
			mainMenu.MenuSubtitle = dealer.ParentBusiness.Name;
			mainMenu.RefreshIndex();
			long purchasingPower = await Money.GetCachedBalanceOrFetch(AccountType.Bank);
			vehiclesCache = new HashSet<StoredVehicle>(from sv in (await TriggerServerEventAsync<string>("gtacnr:vehicles:getAll", new object[1] { true })).Unjson<List<StoredVehicle>>()
				orderby sv.GarageId, sv.GarageParkIndex
				select sv);
			mainMenu.ClearMenuItems();
			if (InventoryMenuScript.Cache == null)
			{
				await InventoryMenuScript.ReloadInventory();
			}
			if (!dealer.IsRental)
			{
				IEnumerable<InventoryEntry> enumerable = InventoryMenuScript.Cache?.Where(delegate(InventoryEntry e)
				{
					if (e.Amount < 1f)
					{
						return false;
					}
					InventoryItem itemDefinition = Gtacnr.Data.Items.GetItemDefinition(e.ItemId);
					if (itemDefinition == null || itemDefinition.ExtraData == null)
					{
						return false;
					}
					return itemDefinition.ExtraData.ContainsKey("GiftCardType") && (string)itemDefinition.ExtraData["GiftCardType"] == "dealership";
				});
				if (enumerable != null && enumerable.Count() > 0)
				{
					giftCardsMenu = new Menu(LocalizationController.S(Entries.Businesses.MENU_STORE_TITLE), LocalizationController.S(Entries.Businesses.MENU_STORE_REDEEM_GIFT_CARDS))
					{
						PlaySelectSound = false
					};
					giftCardsMenu.OnItemSelect += OnMenuItemSelect;
					MenuItem menuItem = new MenuItem("~b~" + LocalizationController.S(Entries.Businesses.MENU_STORE_REDEEM_GIFT_CARDS))
					{
						Label = "›"
					};
					mainMenu.AddMenuItem(menuItem);
					MenuController.AddSubmenu(mainMenu, giftCardsMenu);
					MenuController.BindMenuItem(mainMenu, giftCardsMenu, menuItem);
					foreach (InventoryEntry item2 in enumerable)
					{
						MenuItem item = new MenuItem(Gtacnr.Data.Items.GetItemDefinition(item2.ItemId).Name ?? "")
						{
							Label = $"{item2.Amount:0.##}",
							ItemData = item2
						};
						giftCardsMenu.AddMenuItem(item);
					}
				}
				long num = purchasingPower;
				purchasingPower = num + await UpdateGiftCardBalance();
			}
			DealershipScript.DealershipSupplies[dealer.Type] = DealershipScript.DealershipSupplies[dealer.Type].OrderBy(delegate(DealershipSupply supply)
			{
				if (!DealershipScript.VehicleModelData.ContainsKey(supply.Vehicle))
				{
					return "Z";
				}
				PersonalVehicleModel personalVehicleModel = DealershipScript.VehicleModelData[supply.Vehicle];
				string text12 = API.GetMakeNameFromVehicleModel((uint)API.GetHashKey(personalVehicleModel.Id));
				if (!string.IsNullOrEmpty(personalVehicleModel.OverrideMake))
				{
					text12 = personalVehicleModel.OverrideMake;
				}
				return (string.IsNullOrWhiteSpace(text12) || !DealershipScript.VehicleMakes.ContainsKey(text12)) ? "Unknown Make" : Game.GetGXTEntry(text12);
			}).ToList();
			if (categoryMenus == null)
			{
				categoryMenus = new Dictionary<string, Menu>();
			}
			else
			{
				categoryMenus.Clear();
			}
			Dictionary<string, MenuItem> dictionary = new Dictionary<string, MenuItem>();
			foreach (DealershipSupply item3 in DealershipScript.DealershipSupplies[dealer.Type])
			{
				try
				{
					if (item3.Unlisted)
					{
						continue;
					}
					if (!DealershipScript.VehicleModelData.ContainsKey(item3.Vehicle))
					{
						Print("Warning: vehicle `" + item3.Vehicle + "` is not defined in `gtacnr_items/data/vehicles`.");
						continue;
					}
					PersonalVehicleModel data = DealershipScript.VehicleModelData[item3.Vehicle];
					string text = API.GetMakeNameFromVehicleModel((uint)API.GetHashKey(data.Id));
					if (!string.IsNullOrEmpty(data.OverrideMake))
					{
						text = data.OverrideMake;
					}
					if (DateTime.UtcNow > data.DisabledDate)
					{
						continue;
					}
					string text2;
					VehicleMakeInfo vehicleMakeInfo;
					if (string.IsNullOrEmpty(text) || !DealershipScript.VehicleMakes.ContainsKey(text))
					{
						Print("Warning: vehicle `" + item3.Vehicle + "` has an invalid make `" + text + "`!");
						text2 = "Unknown Make";
						vehicleMakeInfo = null;
					}
					else
					{
						vehicleMakeInfo = DealershipScript.VehicleMakes[text];
						if (vehicleMakeInfo.Alias != null)
						{
							text = vehicleMakeInfo.Alias;
							vehicleMakeInfo = DealershipScript.VehicleMakes[text];
						}
						text2 = Game.GetGXTEntry(text);
					}
					Menu menu;
					if (!categoryMenus.ContainsKey(text))
					{
						menu = new Menu(dealer.IsRental ? LocalizationController.S(Entries.Businesses.MENU_DEALERSHIP_RENTAL_TITLE) : LocalizationController.S(Entries.Businesses.MENU_DEALERSHIP_TITLE), text2);
						categoryMenus.Add(text, menu);
						MenuItem menuItem2 = (dictionary[text] = new MenuItem(" " + text2)
						{
							LeftIcon = (vehicleMakeInfo?.Icon ?? MenuItem.Icon.NONE),
							ItemData = menu,
							Label = ""
						});
						menu.OnIndexChange += OnMenuIndexChange;
						mainMenu.AddMenuItem(menuItem2);
						MenuController.AddSubmenu(mainMenu, menu);
						MenuController.BindMenuItem(mainMenu, menu, menuItem2);
					}
					else
					{
						menu = categoryMenus[text];
					}
					string text3 = Game.GetGXTEntry(Vehicle.GetModelDisplayName(new Model(data.Id)));
					if (!string.IsNullOrEmpty(data.OverrideModel))
					{
						text3 = data.OverrideModel;
					}
					int num2 = item3.CalculateFinalPrice();
					string text4 = ((purchasingPower >= num2) ? "~g~" : "~r~");
					if ((int)data.MembershipTier > 0 && !dealer.IsRental)
					{
						text4 = "~p~";
					}
					string text5 = text3;
					if (!string.IsNullOrEmpty(data.Variant))
					{
						text5 = text5 + " ~c~(" + data.Variant + ")";
					}
					if (data.CreationDate > MainScript.ServerDateTime.Date)
					{
						continue;
					}
					MenuItem menuItem4 = new MenuItem(text5);
					menuItem4.Description = LocalizationController.S(Entries.Vehicles.DEALERSHIP_PREVIEWING_VEHICLE, text2 + " " + text3);
					menuItem4.Label = (dealer.IsRental ? (text4 + num2.ToCurrencyString() + " ~c~/week") : (text4 + num2.ToCurrencyString()));
					menuItem4.ItemData = item3;
					MenuItem menuItem5 = menuItem4;
					menu.AddMenuItem(menuItem5);
					if (!Gtacnr.Utils.CheckTimePassed(data.CreationDate, TimeSpan.FromDays(30.0)))
					{
						string text6 = "~y~" + LocalizationController.S(Entries.Businesses.ITEM_ATTRIBUTE_NEW);
						menuItem5.Text = menuItem5.Text + " " + text6;
						if (!dictionary[text].Label.Contains(text6))
						{
							MenuItem menuItem6 = dictionary[text];
							menuItem6.Label = menuItem6.Label + " " + text6;
						}
					}
					if (data.DisabledDate > default(DateTime))
					{
						TimeSpan timeSpan = data.DisabledDate - DateTime.UtcNow;
						if (timeSpan.TotalDays > 0.0 && timeSpan.TotalDays <= 30.0)
						{
							string text7 = "~r~" + LocalizationController.S(Entries.Businesses.ITEM_ATTRIBUTE_LIMITED);
							menuItem5.Text = menuItem5.Text + " " + text7;
							menuItem5.Description = menuItem5.Description + " " + LocalizationController.S(Entries.Businesses.ITEM_ATTRIBUTE_LIMITED_DESCR, data.DisabledDate.FormatShortDateString());
							if (!dictionary[text].Label.Contains(text7))
							{
								MenuItem menuItem7 = dictionary[text];
								menuItem7.Label = menuItem7.Label + " " + text7;
							}
						}
					}
					if ((int)data.MembershipTier > 0 && !dealer.IsRental)
					{
						if ((int)membershipTier >= (int)data.MembershipTier)
						{
							if (membershipTier == data.MembershipTier)
							{
								menuItem5.Description = menuItem5.Description + "\n~p~" + LocalizationController.S(Entries.Businesses.ITEM_ATTRIBUTE_UNLOCKED_WITH_MEMBERSHIP, Gtacnr.Utils.GetDescription(membershipTier));
							}
							else
							{
								menuItem4 = menuItem5;
								menuItem4.Description = menuItem4.Description + "\n~p~" + LocalizationController.S(Entries.Businesses.ITEM_ATTRIBUTE_UNLOCKED_WITH_MEMBERSHIP, Gtacnr.Utils.GetDescription(membershipTier)) + " " + LocalizationController.S(Entries.Businesses.ITEM_ATTRIBUTE_UNLOCKED_WITH_MEMBERSHIP_REQUIRES_LOWER_TIER, Gtacnr.Utils.GetDescription(data.MembershipTier));
							}
						}
						else
						{
							menuItem5.Description = menuItem5.Description + "\n~p~" + LocalizationController.S(Entries.Businesses.ITEM_ATTRIBUTE_REQUIRES_MEMBERSHIP, Gtacnr.Utils.GetDescription(data.MembershipTier), ExternalLinks.Collection.Store);
						}
					}
					else if (data.RequiredLevel > 0 && !dealer.IsRental)
					{
						menuItem5.Description = menuItem5.Description + "\n~b~" + LocalizationController.S(Entries.Businesses.ITEM_ATTRIBUTE_UNLOCK_LEVEL, data.RequiredLevel);
					}
					if (!dealer.IsRental)
					{
						if (data.Discounts.Count == 1)
						{
							PersonalVehicleModelDiscount personalVehicleModelDiscount = data.Discounts[0];
							string text8 = $"{personalVehicleModelDiscount.PercentOff * 100.0:0.#}";
							menuItem5.Text = menuItem5.Text + " ~g~" + LocalizationController.S(Entries.Businesses.ITEM_ATTRIBUTE_ON_SALE);
							menuItem5.Description = menuItem5.Description + "\n~g~" + LocalizationController.S(Entries.Businesses.ITEM_SALE_DESCR, personalVehicleModelDiscount.Name, text8, personalVehicleModelDiscount.EndDate.Date.FormatShortDateString());
							string text9 = " ~g~" + LocalizationController.S(Entries.Businesses.CAT_ATTRIBUTE_SALES);
							if (!dictionary[text].Label.Contains(text9))
							{
								dictionary[text].Label += text9;
							}
						}
						else if (data.Discounts.Count > 1)
						{
							menuItem5.Text = menuItem5.Text + " ~g~" + LocalizationController.S(Entries.Businesses.ITEM_ATTRIBUTE_ON_SALE);
							menuItem5.Description = menuItem5.Description + "\n~g~" + LocalizationController.S(Entries.Businesses.CAT_ATTRIBUTE_MULTIPLE_SALES);
							string text10 = " ~g~" + LocalizationController.S(Entries.Businesses.CAT_ATTRIBUTE_SALES);
							if (!dictionary[text].Label.Contains(text10))
							{
								dictionary[text].Label += text10;
							}
						}
					}
					if (data.HasExtraData("TruckType"))
					{
						string extraDataString = data.GetExtraDataString("TruckType");
						string text11 = ((extraDataString == "SemiTruck") ? "Semi-Truck" : ((extraDataString == "BoxTruck") ? "Box Truck" : extraDataString));
						menuItem5.Description = menuItem5.Description + "\n~b~Delivery Job Type: ~s~" + text11;
					}
					if (!string.IsNullOrWhiteSpace(data.Notice))
					{
						menuItem5.Description = menuItem5.Description + "\n~y~⚠ " + data.Notice;
					}
					if (!string.IsNullOrWhiteSpace(data.Credits))
					{
						menuItem4 = menuItem5;
						menuItem4.Description = menuItem4.Description + "\n~s~ℹ " + LocalizationController.S(Entries.Vehicles.DEALERSHIP_CREDITS_AND_USAGE_RIGHTS) + ": ~b~" + data.Credits;
					}
					if (vehiclesCache.Any((StoredVehicle v) => v.Model == API.GetHashKey(data.Id)))
					{
						menuItem5.Description = menuItem5.Description + "\n~g~" + LocalizationController.S(Entries.Businesses.ITEM_ATTRIBUTE_OWNED);
					}
					MenuController.BindMenuItem(menu, optionsMenu, menuItem5);
				}
				catch (Exception exception)
				{
					Print("Unable to add supply: " + item3.Vehicle);
					Print(exception);
				}
			}
			foreach (string item4 in (from kvp in categoryMenus
				where kvp.Value.GetMenuItems().Count == 0
				select kvp.Key).ToList())
			{
				mainMenu.RemoveMenuItem(dictionary[item4]);
				categoryMenus.Remove(item4);
			}
		}
		catch (Exception exception2)
		{
			Print(exception2);
		}
	}

	private async Task<bool> SetupDealershipView()
	{
		_ = 1;
		try
		{
			if (currentDealership == null)
			{
				return false;
			}
			Dealership dealer = currentDealership;
			float[] array = dealer.Cameras["FrontCam"];
			Vector4 carPosition = dealer.CarPosition;
			API.DestroyAllCams(true);
			dealerCamera = API.CreateCamWithParams("DEFAULT_SCRIPTED_CAMERA", array[0], array[1], array[2], 0f, 0f, 0f, 45f, false, 0);
			API.SetCamActive(dealerCamera, true);
			API.PointCamAtCoord(dealerCamera, carPosition.X, carPosition.Y, carPosition.Z);
			API.RenderScriptCams(true, false, 2000, true, true);
			AntiTeleportScript.JustTeleported();
			((Entity)Game.PlayerPed).PositionNoOffset = dealer.PlayerLookPosition.XYZ();
			((Entity)Game.PlayerPed).Heading = dealer.PlayerLookPosition.W;
			await Game.PlayerPed.Task.PlayAnimation("amb@world_human_hang_out_street@male_c@idle_a", "idle_b", 1.5f, 1.5f, -1, (AnimationFlags)51, 0.4f);
			((Entity)Game.PlayerPed).IsPositionFrozen = true;
			if ((Entity)(object)dealershipCarMenuPed != (Entity)null && dealershipCarMenuPed.Exists())
			{
				((PoolObject)dealershipCarMenuPed).Delete();
			}
			string key = dealer.Type.ToString();
			dealershipCarMenuPed = await World.CreatePed(DealershipScript.GetModelFromString(DealershipScript.DealershipTypes[key].Ped), dealer.DealerLookPosition.XYZ(), dealer.DealerLookPosition.W);
			dealershipCarMenuPed.AlwaysKeepTask = true;
			dealershipCarMenuPed.BlockPermanentEvents = true;
			API.TaskStartScenarioInPlace(((PoolObject)dealershipCarMenuPed).Handle, "WORLD_HUMAN_CLIPBOARD", 0, true);
			((Entity)dealershipCarMenuPed).PositionNoOffset = dealer.DealerLookPosition.XYZ();
			((Entity)dealershipCarMenuPed).Heading = dealer.DealerLookPosition.W;
			return true;
		}
		catch (Exception exception)
		{
			Print(exception);
			return false;
		}
	}

	private void StopDealershipView(bool killPed = true)
	{
		if (dealerCamera != 0)
		{
			API.SetCamActive(dealerCamera, false);
			API.DestroyCam(dealerCamera, false);
			API.RenderScriptCams(false, false, 0, true, false);
		}
		((Entity)Game.PlayerPed).IsPositionFrozen = false;
		if (killPed && (Entity)(object)dealershipCarMenuPed != (Entity)null && dealershipCarMenuPed.Exists())
		{
			dealershipCarMenuPed.Task.ClearAllImmediately();
			((PoolObject)dealershipCarMenuPed).Delete();
		}
		API.SetGameplayCamRelativePitch(0f, 1f);
		API.SetGameplayCamRelativeHeading(0f);
	}

	private async Task SetSelectedVehicle(PersonalVehicleModel vehicleModelData)
	{
		while (isLoadingVehiclePreview)
		{
			await BaseScript.Delay(0);
		}
		isLoadingVehiclePreview = true;
		try
		{
			if ((Entity)(object)selectedVehicle != (Entity)null && selectedVehicle.Exists())
			{
				((PoolObject)selectedVehicle).Delete();
			}
			if (!DealershipScript.IsInDealership || currentDealership == null)
			{
				return;
			}
			Vector4 carLoc = currentDealership.CarPosition;
			uint hashKey = (uint)API.GetHashKey(vehicleModelData.Id);
			using DisposableModel vehModel = new DisposableModel(hashKey)
			{
				TimeOut = TimeSpan.FromSeconds(10.0)
			};
			await vehModel.Load();
			selectedVehicle = await World.CreateVehicle(vehModel.Model, carLoc.XYZ(), carLoc.W);
			selectedVehicleModelData = vehicleModelData;
			VehicleColorSet vehicleColorSet = ((selectedSupply.Colors != null) ? selectedSupply.Colors : selectedVehicleModelData.Colors);
			List<int> list = ((selectedSupply.Liveries != null) ? selectedSupply.Liveries : selectedVehicleModelData.Liveries);
			if (vehicleColorSet.Primary.Count >= 1)
			{
				selectedVehicle.Mods.PrimaryColor = (VehicleColor)vehicleColorSet.Primary.First();
			}
			if (vehicleColorSet.Secondary.Count >= 1)
			{
				selectedVehicle.Mods.SecondaryColor = (VehicleColor)vehicleColorSet.Secondary.First();
			}
			else
			{
				selectedVehicle.Mods.SecondaryColor = selectedVehicle.Mods.PrimaryColor;
			}
			if (vehicleColorSet.Dashboard.Count >= 1)
			{
				selectedVehicle.Mods.DashboardColor = (VehicleColor)vehicleColorSet.Dashboard.First();
			}
			if (vehicleColorSet.Trim.Count >= 1)
			{
				selectedVehicle.Mods.TrimColor = (VehicleColor)vehicleColorSet.Trim.First();
			}
			if (list.Count >= 1)
			{
				selectedVehicle.Mods.Livery = list.First();
			}
			else
			{
				selectedVehicle.Mods.Livery = 0;
			}
			selectedVehicle.Mods.PearlescentColor = (VehicleColor)0;
			selectedVehicle.DirtLevel = 0f;
		}
		catch (Exception exception)
		{
			Print("^1Unable to preview the following vehicle: " + vehicleModelData.Id);
			Print(exception);
		}
		finally
		{
			isLoadingVehiclePreview = false;
		}
	}

	[EventHandler("gtacnr:dealership:forceExitDealership")]
	private void OnForceExitDealership()
	{
		SetIsInTestDrive(toggle: false);
		SetIsInDealershipMenu(toggle: false);
		if ((Entity)(object)selectedVehicle != (Entity)null && selectedVehicle.Exists())
		{
			((PoolObject)selectedVehicle).Delete();
		}
		StopDealershipView();
		DealershipScript.IsInDealership = false;
	}

	private void OpenGaragesMenu()
	{
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			MembershipTier currentMembershipTier = MembershipScript.GetCurrentMembershipTier();
			garagesMenu.ClearMenuItems();
			foreach (Garage garage in GarageScript.OwnedGarages)
			{
				string locationName = Utils.GetLocationName(garage.OnFootPosition);
				string vehicleFullName = Utils.GetVehicleFullName(selectedVehicleModelData.Id);
				int num = vehiclesCache.Where((StoredVehicle v) => v.GarageId == garage.Id).Count();
				bool flag = (int)currentMembershipTier >= (int)garage.MembershipTier;
				MenuItem menuItem = new MenuItem(garage.Name ?? "");
				menuItem.Description = LocalizationController.S(Entries.Vehicles.DEALERSHIP_STORE_VEHICLE_IN_GARAGE, vehicleFullName, garage.Name, locationName);
				menuItem.ItemData = garage;
				menuItem.Label = $"{num} ~b~of {garage.Interior.ParkingSpaces.Count}";
				menuItem.Enabled = flag;
				menuItem.RightIcon = ((!flag) ? MenuItem.Icon.LOCK : MenuItem.Icon.NONE);
				MenuItem menuItem2 = menuItem;
				if (!flag)
				{
					menuItem2.Description = menuItem2.Description + "\n~p~" + LocalizationController.S(Entries.Businesses.ITEM_ATTRIBUTE_REQUIRES_MEMBERSHIP, Gtacnr.Utils.GetDescription(garage.MembershipTier), ExternalLinks.Collection.Store);
				}
				if (num == garage.Interior.ParkingSpaces.Count)
				{
					menuItem2.Enabled = false;
					menuItem2.Label = $"{num} of {garage.Interior.ParkingSpaces.Count}";
				}
				garagesMenu.AddMenuItem(menuItem2);
			}
			MenuController.BindMenuItem(optionsMenu, garagesMenu, menuItems["purchase"]);
			garagesMenu.OpenMenu();
		}
		catch (Exception exception)
		{
			Print(exception);
		}
	}

	public static async Task<string?> ValidateVanityPlate(string plate, bool sendNotifications = true, bool playSounds = true)
	{
		return await instance.ValidateVanityPlateInternal(plate, sendNotifications, playSounds);
	}

	private async Task<string?> ValidateVanityPlateInternal(string plate, bool sendNotifications = true, bool playSounds = true)
	{
		plate = plate.ToUpperInvariant().Trim();
		foreach (string bannedWord in BadWordsScript.BannedWords)
		{
			if (plate.Contains(bannedWord.ToUpperInvariant()))
			{
				if (sendNotifications)
				{
					Utils.SendNotification(LocalizationController.S(Entries.Imenu.IMENU_DEALERSHIP_PLATE_FORBIDDEN_WORD));
				}
				if (playSounds)
				{
					Utils.PlayErrorSound();
				}
				return null;
			}
		}
		if (plate == "ERROR")
		{
			if (sendNotifications)
			{
				Utils.SendNotification(LocalizationController.S(Entries.Imenu.IMENU_DEALERSHIP_PLATE_ERROR));
			}
			if (playSounds)
			{
				Utils.PlayErrorSound();
			}
			return null;
		}
		if (!plate.All(Gtacnr.Utils.IsAlphanumeric))
		{
			if (sendNotifications)
			{
				Utils.SendNotification(LocalizationController.S(Entries.Imenu.IMENU_DEALERSHIP_PLATE_INVALID_CHARACTERS));
			}
			if (playSounds)
			{
				Utils.PlayErrorSound();
			}
			return null;
		}
		if (!(await TriggerServerEventAsync<bool>("gtacnr:vehicles:isPlateAvailable", new object[1] { plate })))
		{
			if (sendNotifications)
			{
				Utils.SendNotification(LocalizationController.S(Entries.Imenu.IMENU_DEALERSHIP_PLATE_TAKEN));
			}
			if (playSounds)
			{
				Utils.PlayErrorSound();
			}
			return null;
		}
		if (playSounds)
		{
			Utils.PlaySelectSound();
		}
		return plate;
	}
}
