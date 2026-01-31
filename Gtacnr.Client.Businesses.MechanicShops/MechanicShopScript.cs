using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Gtacnr.Client.API;
using Gtacnr.Client.Businesses.Dealerships;
using Gtacnr.Client.HUD;
using Gtacnr.Client.IMenu;
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

namespace Gtacnr.Client.Businesses.MechanicShops;

public class MechanicShopScript : Script
{
	private Menu mechanicMenu;

	private Dictionary<string, MenuItem> menuItems = new Dictionary<string, MenuItem>();

	private Dictionary<string, MechanicTypeMetadata> mechanicTypes = Gtacnr.Utils.LoadJson<Dictionary<string, MechanicTypeMetadata>>("data/mechanic/mechanicTypes.json");

	private Dictionary<string, int> certificationPrices = Gtacnr.Utils.LoadJson<Dictionary<string, int>>("data/mechanic/certifications.json");

	private MechanicShop currentMechanicShop;

	private Vector3 menuCoords;

	private bool canOpenMenu;

	private bool refreshTaskAttached;

	private bool isBusy;

	private static MechanicShopScript instance;

	public static Dictionary<string, MechanicTypeMetadata> MechanicTypes => instance.mechanicTypes;

	public MechanicShopScript()
	{
		instance = this;
		CreateMechanicMenus();
	}

	public static MechanicShop GetClosestMechanicShop()
	{
		return BusinessScript.Businesses.Values.Where((Business b) => b.Mechanic != null).OrderBy(delegate(Business b)
		{
			//IL_0005: Unknown result type (might be due to invalid IL or missing references)
			//IL_000a: Unknown result type (might be due to invalid IL or missing references)
			//IL_000e: Unknown result type (might be due to invalid IL or missing references)
			Vector3 position = ((Entity)Game.PlayerPed).Position;
			return ((Vector3)(ref position)).DistanceToSquared2D(b.Location);
		}).First()
			.Mechanic;
	}

	private void CreateMechanicMenus()
	{
		mechanicMenu = new Menu(LocalizationController.S(Entries.Businesses.MENU_MECHANIC_TITLE), LocalizationController.S(Entries.Businesses.MENU_MECHANIC_TITLE))
		{
			MaxDistance = 15f
		};
		mechanicMenu.OnMenuOpen += OnMenuOpen;
		mechanicMenu.OnItemSelect += OnItemSelect;
		MenuController.AddMenu(mechanicMenu);
	}

	public static void OpenMechanicMenu(Business business)
	{
		instance._OpenMechanicMenu(business);
	}

	public static void OpenMechanicMenu(MechanicShop mechShop)
	{
		instance._OpenMechanicMenu(mechShop.ParentBusiness);
	}

	private async void _OpenMechanicMenu(Business business)
	{
		_ = 3;
		try
		{
			if (business == null)
			{
				business = BusinessScript.ClosestBusiness;
			}
			if (business?.Mechanic == null)
			{
				return;
			}
			currentMechanicShop = business?.Mechanic;
			Menu menu = mechanicMenu;
			menu.MenuSubtitle = business.Name;
			menu.OpenMenu();
			menu.ClearMenuItems();
			menu.AddLoadingMenuItem();
			long money = await Money.GetCachedBalanceOrFetch(AccountType.Cash);
			string jobId = await Gtacnr.Client.API.Jobs.GetCurrentJobId();
			IEnumerable<InventoryEntry> primaryInv = InventoryMenuScript.Cache;
			if (primaryInv == null || primaryInv.Count() == 0)
			{
				primaryInv = await InventoryMenuScript.ReloadInventory();
			}
			MembershipTier playerTier = MembershipScript.GetCurrentMembershipTier();
			menu.ClearMenuItems();
			int fullRepairPrice = 0;
			int quickRepairPrice = 0;
			int tiresRepairPrice = 0;
			if (Gtacnr.Client.API.Jobs.CachedJobEnum == JobsEnum.Mechanic && currentMechanicShop.SalePercentage > 0f)
			{
				menu.CounterPreText = LocalizationController.S(Entries.Businesses.MENU_MECHANIC_SHOP_PRETEXT, $"{currentMechanicShop.SalePercentage * 100f:0.##}");
			}
			string key = currentMechanicShop.Type.ToString();
			if (mechanicTypes.TryGetValue(key, out MechanicTypeMetadata mechType))
			{
				Vehicle vehicle = (Game.PlayerPed.IsInVehicle() ? Game.PlayerPed.CurrentVehicle : Game.PlayerPed.LastVehicle);
				bool disable = false;
				string disableReason = "";
				await BaseScript.Delay(0);
				List<Player> list = (from p in ((BaseScript)this).Players.GetPlayersInRange(business.Location, 50f)
					where LatentPlayers.Get(p).JobEnum == JobsEnum.Mechanic && p.ServerId != Game.Player.ServerId
					select p).ToList();
				if ((Entity)(object)vehicle != (Entity)null)
				{
					Vector3 position = ((Entity)vehicle).Position;
					if (((Vector3)(ref position)).DistanceToSquared(business.Location) > 30f.Square())
					{
						vehicle = null;
					}
				}
				if ((Entity)(object)vehicle != (Entity)null)
				{
					if (mechType.BlacklistedClasses != null)
					{
						foreach (int blacklistedClass in mechType.BlacklistedClasses)
						{
							if ((int)vehicle.ClassType == blacklistedClass)
							{
								disable = true;
								break;
							}
						}
					}
					if (mechType.WhitelistedClasses != null)
					{
						disable = true;
						foreach (int whitelistedClass in mechType.WhitelistedClasses)
						{
							if ((int)vehicle.ClassType == whitelistedClass)
							{
								disable = false;
								break;
							}
						}
					}
					if (disable)
					{
						disableReason = LocalizationController.S(Entries.Jobs.MECHANIC_INVALID_TYPE);
					}
					float multiplier = (business.PriceMultipliers.ContainsKey("") ? business.PriceMultipliers[""] : 1f);
					_ = (Entity)(object)ActiveVehicleScript.ActiveVehicle != (Entity)(object)vehicle;
					fullRepairPrice = GetVehicleRepairCost(vehicle, mechType.RepairType, multiplier);
					quickRepairPrice = GetVehicleRepairCost(vehicle, mechType.RepairType, multiplier, MechanicRepairtype.Quick);
					tiresRepairPrice = GetVehicleRepairCost(vehicle, mechType.RepairType, multiplier, MechanicRepairtype.Tires);
				}
				else
				{
					disableReason = LocalizationController.S(Entries.Jobs.MECHANIC_DONT_HAVE_VEHICLE);
					disable = true;
				}
				if (list.Count > 0 && currentMechanicShop.Type == MechanicType.ModShop)
				{
					disableReason = LocalizationController.S(Entries.Jobs.MECHANIC_REAL_PLAYERS_AVAILABLE);
					disable = true;
				}
				Menu menu2 = menu;
				MenuItem item = (menuItems["fullfix"] = new MenuItem(LocalizationController.S(Entries.Jobs.MECHANIC_FULL_REPAIR))
				{
					Description = (disable ? disableReason : LocalizationController.S(Entries.Jobs.MECHANIC_FULL_REPAIR_DESC, GetVehicleRepairTime(MechanicRepairtype.Full).TotalSeconds.ToInt())),
					Label = ((fullRepairPrice <= -1) ? LocalizationController.S(Entries.Businesses.STP_SERVICE_FIX_NA) : ((fullRepairPrice == 0) ? "N/A" : fullRepairPrice.ToPriceTagString(money))),
					Enabled = (fullRepairPrice > 0 && money >= fullRepairPrice && !disable)
				});
				menu2.AddMenuItem(item);
				Menu menu3 = menu;
				item = (menuItems["quickfix"] = new MenuItem(LocalizationController.S(Entries.Jobs.MECHANIC_QUICK_REPAIR))
				{
					Description = (disable ? disableReason : LocalizationController.S(Entries.Jobs.MECHANIC_QUICK_REPAIR_DESC, GetVehicleRepairTime(MechanicRepairtype.Quick).TotalSeconds.ToInt())),
					Label = ((quickRepairPrice <= -1) ? LocalizationController.S(Entries.Businesses.STP_SERVICE_FIX_NA) : ((quickRepairPrice == 0) ? "N/A" : quickRepairPrice.ToPriceTagString(money))),
					Enabled = (quickRepairPrice > 0 && money >= quickRepairPrice && !disable)
				});
				menu3.AddMenuItem(item);
				Menu menu4 = menu;
				item = (menuItems["tiresfix"] = new MenuItem(LocalizationController.S(Entries.Jobs.MECHANIC_TIRE_REPAIR))
				{
					Description = (disable ? disableReason : LocalizationController.S(Entries.Jobs.MECHANIC_TIRE_REPAIR_DESC, GetVehicleRepairTime(MechanicRepairtype.Tires).TotalSeconds.ToInt())),
					Label = ((tiresRepairPrice <= -1) ? LocalizationController.S(Entries.Businesses.STP_SERVICE_FIX_NA) : ((tiresRepairPrice == 0) ? "N/A" : tiresRepairPrice.ToPriceTagString(money))),
					Enabled = (tiresRepairPrice > 0 && money >= tiresRepairPrice && !disable)
				});
				menu4.AddMenuItem(item);
				if (currentMechanicShop.Type == MechanicType.ModShop)
				{
					Menu menu5 = menu;
					item = (menuItems["plate"] = new MenuItem(LocalizationController.S(Entries.Businesses.MENU_MECHANIC_SHOP_PLATE)));
					menu5.AddMenuItem(item);
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
				else if (currentMechanicShop.Type == MechanicType.HelicopterRepairStation)
				{
					AddSpecialRepairCertificateItem("heli_repair_certificate");
				}
				else if (currentMechanicShop.Type == MechanicType.PlaneRepairStation)
				{
					AddSpecialRepairCertificateItem("plane_repair_certificate");
				}
				if (jobId == "mechanic")
				{
					Menu menu6 = menu;
					item = (menuItems["toCivilian"] = Gtacnr.Data.Jobs.GetJobData("none").ToSwitchMenuItem());
					menu6.AddMenuItem(item);
				}
				else
				{
					Menu menu7 = menu;
					item = (menuItems["toMechanic"] = Gtacnr.Data.Jobs.GetJobData("mechanic").ToSwitchMenuItem());
					menu7.AddMenuItem(item);
				}
			}
			else
			{
				mechanicMenu.AddMenuItem(new MenuItem(LocalizationController.S(Entries.Main.MENU_ERROR_ITEM), "This mechanic shop's data is invalid or corrupted."));
			}
			void AddSpecialRepairCertificateItem(string itemId)
			{
				InventoryItem itemDefinition = Gtacnr.Data.Items.GetItemDefinition(itemId);
				bool flag = primaryInv.Any((InventoryEntry e) => e?.ItemId == itemId);
				int value;
				bool flag2 = certificationPrices.TryGetValue(itemId, out value);
				if (jobId == "mechanic" && itemDefinition != null && flag2)
				{
					Menu menu8 = menu;
					Dictionary<string, MenuItem> dictionary = menuItems;
					string key2 = itemId;
					MenuItem obj = new MenuItem(itemDefinition.Name)
					{
						Description = itemDefinition.Description,
						Label = (flag ? "OWNED" : value.ToPriceTagString(money)),
						Enabled = !flag,
						ItemData = itemId
					};
					MenuItem item2 = obj;
					dictionary[key2] = obj;
					menu8.AddMenuItem(item2);
				}
			}
		}
		catch (Exception ex)
		{
			Print(ex);
			mechanicMenu.ClearMenuItems();
			mechanicMenu.AddErrorMenuItem(ex);
		}
	}

	private void OnMenuOpen(Menu menu)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		menu.MenuCoords = menuCoords;
	}

	private async void OnItemSelect(Menu menu, MenuItem menuItem, int itemIndex)
	{
		if (isBusy)
		{
			return;
		}
		if (IsSelected("quickfix") || IsSelected("tiresfix") || IsSelected("fullfix"))
		{
			Vehicle vehicle = (Game.PlayerPed.IsInVehicle() ? Game.PlayerPed.CurrentVehicle : Game.PlayerPed.LastVehicle);
			if ((Entity)(object)vehicle == (Entity)null)
			{
				Utils.DisplayHelpText(LocalizationController.S(Entries.Jobs.MECHANIC_DONT_HAVE_VEHICLE_REPAIR));
				return;
			}
			MenuController.CloseAllMenus();
			API.BringVehicleToHalt(((PoolObject)vehicle).Handle, 1f, 0, false);
			await BaseScript.Delay(200);
			vehicle.Speed = 0f;
			((Entity)vehicle).Velocity = default(Vector3);
			if (IsSelected("fullfix"))
			{
				await RepairVehicle(vehicle, MechanicRepairtype.Full);
			}
			else if (IsSelected("tiresfix"))
			{
				await RepairVehicle(vehicle, MechanicRepairtype.Tires);
			}
			else
			{
				await RepairVehicle(vehicle, MechanicRepairtype.Quick);
			}
			return;
		}
		if (IsSelected("toCivilian"))
		{
			await Gtacnr.Client.API.Jobs.TrySwitch("none", "default", null, BeforeSwitching, AfterSwitching);
			return;
		}
		if (IsSelected("toMechanic"))
		{
			await Gtacnr.Client.API.Jobs.TrySwitch("mechanic", "default", LocalizationController.S(Entries.Jobs.MECHANIC_DESCRIPTION), BeforeSwitching, AfterSwitching);
			return;
		}
		string text = menuItem.ItemData as string;
		switch (text)
		{
		case "heli_repair_certificate":
		case "plane_repair_certificate":
			try
			{
				InventoryItem itemData = Gtacnr.Data.Items.GetItemDefinition(text);
				int num = await TriggerServerEventAsync<int>("gtacnr:mechanic:getCertification", new object[1] { text });
				switch (num)
				{
				case 1:
					mechanicMenu.CloseMenu();
					Utils.DisplayHelpText(LocalizationController.S(Entries.Jobs.MECHANIC_CERT_OBTAINED, itemData.Name));
					break;
				case 6:
					Utils.DisplayHelpText(LocalizationController.S(Entries.Main.NOT_ENOUGH_MONEY));
					break;
				default:
					Utils.DisplayErrorMessage(135, num);
					break;
				}
				return;
			}
			catch (Exception exception)
			{
				Print(exception);
				Utils.DisplayErrorMessage(135);
				return;
			}
		}
		if (!IsSelected("plate"))
		{
			return;
		}
		Vehicle vehicle2 = Game.PlayerPed.CurrentVehicle ?? Game.PlayerPed.LastVehicle;
		if ((Entity)(object)vehicle2 == (Entity)null)
		{
			Utils.DisplayHelpText(LocalizationController.S(Entries.Jobs.MECHANIC_DONT_HAVE_VEHICLE));
			return;
		}
		string text2 = LatentVehicleStateScript.Get(((Entity)vehicle2).NetworkId)?.PersonalVehicleId;
		if (text2 != null && text2 != ActiveVehicleScript.ActiveVehicleStoredId)
		{
			Utils.DisplayHelpText(LocalizationController.S(Entries.Jobs.MECHANIC_CANT_CHANGE_PLATE_OTHERS));
			return;
		}
		string input = await Utils.GetUserInput(LocalizationController.S(Entries.Jobs.MECHANIC_PLATE_INPUT_TITLE), LocalizationController.S(Entries.Jobs.MECHANIC_PLATE_INPUT_TEXT), "", 8);
		if (input == null)
		{
			return;
		}
		string text3 = await DealershipMenuScript.ValidateVanityPlate(input);
		if (string.IsNullOrEmpty(text3))
		{
			return;
		}
		input = text3;
		if ((Entity)(object)vehicle2 == (Entity)(object)ActiveVehicleScript.ActiveVehicle)
		{
			UpdatePlateResponse updatePlateResponse = (UpdatePlateResponse)(await TriggerServerEventAsync<int>("gtacnr:vehicles:updatePlate", new object[2]
			{
				ActiveVehicleScript.ActiveVehicleStoredId,
				input
			}));
			switch (updatePlateResponse)
			{
			case UpdatePlateResponse.Success:
				Utils.SendNotification(LocalizationController.S(Entries.Jobs.MECHANIC_PLATE_SET_SAVED, input));
				Utils.PlaySelectSound();
				SetPlate();
				break;
			case UpdatePlateResponse.InvalidVehicle:
				Utils.SendNotification(LocalizationController.S(Entries.Jobs.MECHANIC_PLATE_STOLEN_CAR));
				Utils.PlayErrorSound();
				break;
			case UpdatePlateResponse.RentedVehicle:
				Utils.SendNotification(LocalizationController.S(Entries.Jobs.MECHANIC_PLATE_RENTED_CAR));
				Utils.PlayErrorSound();
				break;
			default:
				Utils.DisplayErrorMessage(129, (int)updatePlateResponse);
				break;
			}
		}
		else
		{
			SetPlate();
			Utils.SendNotification(LocalizationController.S(Entries.Jobs.MECHANIC_PLATE_SET, input));
		}
		async Task AfterSwitching()
		{
			await Utils.FadeIn(500);
			_OpenMechanicMenu(null);
			isBusy = false;
		}
		async Task BeforeSwitching()
		{
			isBusy = true;
			MenuController.CloseAllMenus();
			await Utils.FadeOut(500);
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

	private async Task RepairVehicle(Vehicle vehicle, MechanicRepairtype repairType)
	{
		if (isBusy)
		{
			return;
		}
		try
		{
			isBusy = true;
			DateTime t = DateTime.UtcNow;
			TimeSpan repairTime = GetVehicleRepairTime(repairType);
			Vector3 p = ((Entity)vehicle).Position;
			BarTimerBar progBar = new BarTimerBar("REPAIR")
			{
				Percentage = 0f,
				TextColor = TextColors.Blue,
				Color = BarColors.Blue
			};
			if (vehicle.Doors.HasDoor((VehicleDoorIndex)4))
			{
				vehicle.Doors[(VehicleDoorIndex)4].Open(false, false);
			}
			try
			{
				Utils.DisplayHelpText(LocalizationController.S(Entries.Jobs.MECHANIC_VEHICLE_REPAIR_IN_PROGRESS));
				TimerBarScript.AddTimerBar(progBar);
				while (!Gtacnr.Utils.CheckTimePassed(t, repairTime))
				{
					await BaseScript.Delay(50);
					TimeSpan cooldownTimeLeft = Gtacnr.Utils.GetCooldownTimeLeft(t, repairTime);
					progBar.Percentage = (float)((repairTime.TotalMilliseconds - cooldownTimeLeft.TotalMilliseconds) / repairTime.TotalMilliseconds);
					Vector3 position = ((Entity)vehicle).Position;
					if (((Vector3)(ref position)).DistanceToSquared2D(p) >= 2.25f)
					{
						Utils.DisplayHelpText(LocalizationController.S(Entries.Jobs.MECHANIC_REPAIR_CANCELED));
						return;
					}
					if (currentMechanicShop == null)
					{
						Utils.DisplayHelpText(LocalizationController.S(Entries.Main.UNEXPECTED_ERROR_CODE, "0x54"));
						return;
					}
				}
			}
			finally
			{
				TimerBarScript.RemoveTimerBar(progBar);
				if (vehicle.Doors.HasDoor((VehicleDoorIndex)4))
				{
					vehicle.Doors[(VehicleDoorIndex)4].Close(false);
				}
			}
			ResponseCode responseCode = await TriggerServerEventAsync("gtacnr:mechanic:fixVehicle", ((Entity)vehicle).NetworkId, currentMechanicShop.ParentBusiness.Id, (byte)repairType, GetDamagedTiresNum(vehicle));
			switch (responseCode)
			{
			case ResponseCode.Success:
				switch (repairType)
				{
				case MechanicRepairtype.Quick:
					((dynamic)((BaseScript)this).Exports["vehicle-failure"]).SetVehicleEngineHealth(((PoolObject)vehicle).Handle, Math.Max(750f, vehicle.EngineHealth));
					break;
				case MechanicRepairtype.Tires:
					if (vehicle.Wheels != null)
					{
						for (int k = 0; k < 16; k++)
						{
							API.SetVehicleTyreFixed(((PoolObject)vehicle).Handle, k);
							API.SetVehicleWheelHealth(((PoolObject)vehicle).Handle, k, 1000f);
						}
					}
					break;
				case MechanicRepairtype.Full:
					vehicle.Repair();
					DisableMountedGunsScript.DisableMountedGuns(vehicle);
					vehicle.Wash();
					API.SetVehicleBodyHealth(((PoolObject)vehicle).Handle, 1000f);
					API.SetVehicleEngineHealth(((PoolObject)vehicle).Handle, 1000f);
					API.SetVehiclePetrolTankHealth(((PoolObject)vehicle).Handle, 1000f);
					if (vehicle.Windows != null && vehicle.Windows.HasWindow((VehicleWindowIndex)0) && !vehicle.Windows.AreAllWindowsIntact)
					{
						VehicleWindow[] allWindows = vehicle.Windows.GetAllWindows();
						for (int i = 0; i < allWindows.Length; i++)
						{
							allWindows[i].Repair();
						}
					}
					if (vehicle.Wheels != null)
					{
						for (int j = 0; j < 16; j++)
						{
							API.SetVehicleTyreFixed(((PoolObject)vehicle).Handle, j);
							API.SetVehicleWheelHealth(((PoolObject)vehicle).Handle, j, 1000f);
						}
					}
					break;
				}
				Utils.DisplayHelpText(LocalizationController.S(Entries.Jobs.MECHANIC_VEHICLE_REPAIRED));
				break;
			case ResponseCode.InsufficientMoney:
				Utils.DisplayHelpText(LocalizationController.S(Entries.Main.NOT_ENOUGH_MONEY));
				break;
			case ResponseCode.TooFar:
				Utils.DisplayHelpText(LocalizationController.S(Entries.Jobs.MECHANIC_TOO_FAR_REPAIRPOINT));
				break;
			case ResponseCode.Cooldown:
				Utils.DisplayHelpText(LocalizationController.S(Entries.Jobs.MECHANIC_REPAIRED_TOO_RECENTLY));
				break;
			default:
				Utils.DisplayHelpText(LocalizationController.S(Entries.Main.UNEXPECTED_ERROR_CODE, $"0x55-{(int)responseCode}"));
				break;
			}
		}
		catch (Exception exception)
		{
			Utils.DisplayHelpText(LocalizationController.S(Entries.Main.UNEXPECTED_ERROR_CODE, "0x56"));
			Print(exception);
		}
		finally
		{
			isBusy = false;
		}
	}

	private static uint GetDamagedTiresNum(Vehicle vehicle)
	{
		uint num = 0u;
		if (vehicle.Wheels != null)
		{
			for (int i = 0; i < 16; i++)
			{
				num += (API.IsVehicleTyreBurst(((PoolObject)vehicle).Handle, i, false) ? 1u : 0u);
			}
		}
		return num;
	}

	private int GetVehicleRepairCost(Vehicle vehicle, int vehicleType, float multiplier = 1f, MechanicRepairtype repairtype = MechanicRepairtype.Full)
	{
		return Convert.ToInt32(Math.Ceiling((float)Gtacnr.Utils.CalculateRepairPrice(vehicle.EngineHealth, vehicle.BodyHealth, vehicle.PetrolTankHealth, vehicleType, GetDamagedTiresNum(vehicle), repairtype) * multiplier));
	}

	private TimeSpan GetVehicleRepairTime(MechanicRepairtype repairType)
	{
		return TimeSpan.FromSeconds(repairType switch
		{
			MechanicRepairtype.Full => 7, 
			MechanicRepairtype.Tires => 2, 
			MechanicRepairtype.Quick => 3, 
			_ => 0, 
		});
	}
}
