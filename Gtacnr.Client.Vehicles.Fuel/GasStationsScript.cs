using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Gtacnr.Client.API;
using Gtacnr.Client.API.Scripts;
using Gtacnr.Client.API.UI;
using Gtacnr.Client.Businesses.Dealerships;
using Gtacnr.Client.HUD;
using Gtacnr.Localization;
using Gtacnr.Model;
using Gtacnr.Model.Enums;
using MenuAPI;
using NativeUI;

namespace Gtacnr.Client.Vehicles.Fuel;

public class GasStationsScript : Script
{
	private static List<GasStation> gasStations = Gtacnr.Utils.LoadJson<List<GasStation>>("data/vehicles/gasStations.json");

	private static Dictionary<string, int> gasPrices = Gtacnr.Utils.LoadJson<Dictionary<string, int>>("data/vehicles/gasPrices.json");

	private static Dictionary<string, Menu> menus = new Dictionary<string, Menu>();

	private static Dictionary<string, MenuItem> menuItems = new Dictionary<string, MenuItem>();

	private static GasStation currentGasStation;

	private static int currentPumpIdx;

	private bool canOpenPumpMenu;

	private bool isBusy;

	private Vehicle selectedVehicle;

	public static IEnumerable<GasStation> GasStations => gasStations;

	public static IReadOnlyDictionary<string, int> GasPrices => gasPrices;

	public static GasStation CurrentGasStation => currentGasStation;

	public static int CurrentPumpIndex => currentPumpIdx;

	protected override void OnStarted()
	{
		CreateMenus();
		LoadGasStations();
	}

	private void LoadGasStations()
	{
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		foreach (GasStation gasStation in gasStations)
		{
			if (gasStation.CreateBlip)
			{
				Blip val = World.CreateBlip(gasStation.Position);
				val.Sprite = (BlipSprite)361;
				val.Color = (BlipColor)6;
				Utils.SetBlipName(val, LocalizationController.S(Entries.Businesses.BUSINESS_GAS_STATION), "gas_station");
				val.Scale = 0.7f;
				val.IsShortRange = true;
			}
		}
	}

	private void CreateMenus()
	{
		menus["pump"] = new Menu(LocalizationController.S(Entries.Businesses.BUSINESS_GAS_STATION), LocalizationController.S(Entries.Main.MENU_CHOOSE_OPTION));
		menus["pump"].PlaySelectSound = false;
		menus["pump"].OnItemSelect += OnMenuItemSelect;
		menus["pump"].OnMenuClose += OnMenuClose;
		MenuController.AddMenu(menus["pump"]);
	}

	private async void OpenPumpMenu()
	{
		try
		{
			Vector4[] array = currentGasStation.Pumps.Concat(currentGasStation.Chargers).ToArray();
			if (currentGasStation == null || currentPumpIdx < 0 || currentPumpIdx >= array.Length || menus["pump"].Visible)
			{
				return;
			}
			Vector4 pump = array[currentPumpIdx];
			bool isEVCharger = currentPumpIdx >= currentGasStation.Pumps.Count;
			if (!((Entity)(object)Game.PlayerPed.CurrentVehicle != (Entity)null))
			{
				Vehicle currentVehicle = Game.PlayerPed.CurrentVehicle;
				if (currentVehicle == null || !currentVehicle.Exists())
				{
					menus["pump"].OpenMenu();
					if (isBusy)
					{
						menus["pump"].CloseMenu();
						Utils.PlayErrorSound();
						return;
					}
					menus["pump"].ClearMenuItems();
					menus["pump"].AddLoadingMenuItem();
					Vehicle vehicle = Game.PlayerPed.LastVehicle;
					int vehPrice = -1;
					if ((Entity)(object)vehicle == (Entity)null || ((Entity)vehicle).Model.IsPlane || ((Entity)vehicle).Model.IsHelicopter || ((Entity)vehicle).Model.IsBicycle || ((Entity)vehicle).Model.IsTrain)
					{
						vehicle = null;
					}
					int num;
					if ((Entity)(object)vehicle != (Entity)null)
					{
						Vehicle obj = vehicle;
						num = ((obj != null && obj.Exists()) ? 1 : 0);
					}
					else
					{
						num = 0;
					}
					bool hasVehicle = (byte)num != 0;
					selectedVehicle = vehicle;
					bool isElectric = (hasVehicle ? DealershipScript.FindVehicleModelData(Model.op_Implicit(((Entity)vehicle).Model)) : null)?.IsElectric ?? false;
					if (isEVCharger && !isElectric)
					{
						menus["pump"].CloseMenu();
						Utils.PlayErrorSound();
						Utils.DisplayHelpText(LocalizationController.S(Entries.Businesses.GAS_STATION_ONLY_EVS), playSound: false);
						return;
					}
					if (!isEVCharger && isElectric)
					{
						menus["pump"].CloseMenu();
						Utils.PlayErrorSound();
						Utils.DisplayHelpText(LocalizationController.S(Entries.Businesses.GAS_STATION_NO_EVS), playSound: false);
						return;
					}
					Animate();
					string key = (isEVCharger ? "ElectricVehicles" : currentGasStation.Type.ToString());
					float pricePerGal = (float)gasPrices[key] * currentGasStation.PriceMultiplier;
					long num2 = await Money.GetCachedBalanceOrFetch(AccountType.Bank);
					bool flag = Game.PlayerPed.Weapons.HasWeapon((WeaponHash)883325847);
					int pedAmmoByType = API.GetPedAmmoByType(((PoolObject)Game.PlayerPed).Handle, -899475295);
					bool flag2 = pedAmmoByType < 1000;
					float num3 = GasScript.JERRYCAN_CAPACITY - (float)pedAmmoByType * GasScript.JERRYCAN_CAPACITY / 1000f;
					int num4 = Convert.ToInt32(Math.Ceiling(num3 * pricePerGal));
					float num5 = 0f;
					if ((Entity)(object)vehicle != (Entity)null)
					{
						Vector3 position = ((Entity)vehicle).Position;
						if (!(((Vector3)(ref position)).DistanceToSquared(((Entity)Game.PlayerPed).Position) > 49f))
						{
							num5 = Utils.GetVehicleRefuelAmount(vehicle);
							vehPrice = Convert.ToInt32(Math.Ceiling(num5 * pricePerGal));
						}
					}
					bool flag3 = num5 >= 0.1f;
					menus["pump"].ClearMenuItems();
					menus["pump"].MenuSubtitle = currentGasStation.Name;
					menus["pump"].CounterPreText = string.Format("${0:0.##}/{1}", pricePerGal, isElectric ? "kWh" : "gal");
					menuItems["buyVeh"] = new MenuItem("Vehicle")
					{
						Description = (isElectric ? $"{LocalizationController.S(Entries.Businesses.GAS_STATION_CHARGE_BATTERY)} ({num5:0.##} kWh). " : $"{LocalizationController.S(Entries.Businesses.GAS_STATION_FILL_TANK)} ({num5:0.##} gal). ") + "~y~" + LocalizationController.S(Entries.Businesses.MENU_BUSINESS_CASHLESS) + ".~s~" + (hasVehicle ? "" : ("~n~" + LocalizationController.S(Entries.Businesses.STP_SERVICE_ERROR_NO_VEHICLE))),
						Label = ((vehPrice < 0) ? "UNAVAILABLE" : ((!flag3) ? "FULL" : vehPrice.ToPriceTagString(num2))),
						Enabled = (hasVehicle && flag3 && num2 >= vehPrice),
						ItemData = num5
					};
					menus["pump"].AddMenuItem(menuItems["buyVeh"]);
					if (!isEVCharger)
					{
						menuItems["buyCan"] = new MenuItem("Jerrycan")
						{
							Description = $"{LocalizationController.S(Entries.Businesses.GAS_STATION_FILL_JERRYCAN)} ({num3:0.##} gal). " + "~y~" + LocalizationController.S(Entries.Businesses.MENU_BUSINESS_CASHLESS) + ".~s~" + (flag ? "" : ("~n~" + Entries.Businesses.GAS_STATION_ERROR_NO_JERRYCAN + ".")),
							Label = ((num4 < 0 || !flag) ? "UNAVAILABLE" : ((!flag2) ? "FULL" : num4.ToPriceTagString(num2))),
							Enabled = (flag && flag2 && num2 >= num4),
							ItemData = num3
						};
						menus["pump"].AddMenuItem(menuItems["buyCan"]);
					}
					return;
				}
			}
			Utils.DisplayHelpText(isEVCharger ? LocalizationController.S(Entries.Vehicles.ON_FOOT_TO_USE_EV_CHARGER) : LocalizationController.S(Entries.Vehicles.ON_FOOT_TO_USE_FUEL_PUMP));
			async void Animate()
			{
				Game.PlayerPed.Task.ClearAll();
				API.TaskAchieveHeading(((PoolObject)Game.PlayerPed).Handle, pump.W, 1000);
				await BaseScript.Delay(1000);
				((Entity)Game.PlayerPed).Heading = pump.W;
				API.TaskStartScenarioInPlace(API.PlayerPedId(), "PROP_HUMAN_ATM", 0, true);
			}
		}
		catch (Exception exception)
		{
			Print(exception);
		}
	}

	private async void OnMenuItemSelect(Menu menu, MenuItem menuItem, int itemIndex)
	{
		if (menu != menus["pump"])
		{
			return;
		}
		if (isBusy)
		{
			Utils.PlayErrorSound();
		}
		else
		{
			if (!IsSelected("buyVeh") && !IsSelected("buyCan"))
			{
				return;
			}
			try
			{
				isBusy = true;
				Utils.PlaySelectSound();
				menuItem.Label = "IN PROGRESS...";
				menuItem.Enabled = false;
				float amount = (float)menuItem.ItemData;
				bool isJerrycan = IsSelected("buyCan");
				Vector4[] array = currentGasStation.Pumps.Concat(currentGasStation.Chargers).ToArray();
				Vector4 pump = array[currentPumpIdx];
				_ = currentPumpIdx;
				_ = currentGasStation.Pumps.Count;
				if (isJerrycan)
				{
					Game.PlayerPed.Task.ClearAll();
					Game.PlayerPed.Weapons.Select((WeaponHash)883325847);
				}
				int num = await TriggerServerEventAsync<int>("gtacnr:fuel:buy", new object[3]
				{
					gasStations.IndexOf(currentGasStation),
					amount,
					""
				});
				if (num != 1)
				{
					MenuController.CloseAllMenus();
					Utils.DisplayErrorMessage(36, num);
					return;
				}
				float max = (isJerrycan ? GasScript.JERRYCAN_CAPACITY : (Utils.GetVehicleTankCapacityL(selectedVehicle) * GasScript.GALLONS_PER_LITER));
				float percentage = 1f - amount / max;
				float currentAmount = max - amount;
				float totalAddedGal = 0f;
				BarTimerBar bar = null;
				PersonalVehicleModel modelData = (isJerrycan ? null : DealershipScript.FindVehicleModelData(Model.op_Implicit(((Entity)selectedVehicle).Model)));
				try
				{
					bar = new BarTimerBar(isJerrycan ? "JERRYCAN" : "VEHICLE")
					{
						Percentage = percentage
					};
					TimerBarScript.AddTimerBar(bar);
					while (currentAmount < max)
					{
						await BaseScript.Delay(100);
						if (isJerrycan)
						{
							Vector3 position = ((Entity)Game.PlayerPed).Position;
							if (((Vector3)(ref position)).DistanceToSquared(pump.XYZ()) > 1.21f)
							{
								break;
							}
						}
						else
						{
							Vector3 position2 = ((Entity)selectedVehicle).Position;
							if (((Vector3)(ref position2)).DistanceToSquared(pump.XYZ()) > 64f)
							{
								break;
							}
						}
						float num2 = ((isJerrycan || (modelData?.IsElectric ?? false)) ? 0.02f : 0.05f);
						currentAmount += num2;
						totalAddedGal += num2;
						if (currentAmount > max)
						{
							currentAmount = max;
						}
						bar.Percentage = currentAmount / max;
						if (!isJerrycan && (Entity)(object)Game.PlayerPed.CurrentVehicle == (Entity)(object)selectedVehicle)
						{
							GasScript.OverrideFuelLevel = currentAmount / max;
						}
					}
				}
				catch (Exception exception)
				{
					Print(exception);
				}
				finally
				{
					TimerBarScript.RemoveTimerBar(bar);
				}
				if (currentAmount < max)
				{
					menuItem.Label = "CANCELED";
				}
				else
				{
					menuItem.Label = "FULL";
				}
				if (isJerrycan)
				{
					BaseScript.TriggerServerEvent("gtacnr:fuel:completed", new object[4] { totalAddedGal, 0, 0f, null });
					return;
				}
				float vehicleTankCapacityL = Utils.GetVehicleTankCapacityL(selectedVehicle);
				string text = Utils.GetVehicleHealthData(selectedVehicle).Json();
				BaseScript.TriggerServerEvent("gtacnr:fuel:completed", new object[4]
				{
					totalAddedGal,
					((Entity)selectedVehicle).NetworkId,
					vehicleTankCapacityL,
					text
				});
				if (!isJerrycan && (Entity)(object)Game.PlayerPed.CurrentVehicle == (Entity)(object)selectedVehicle)
				{
					GasScript.OverrideFuelLevel = currentAmount / max;
				}
				Vector3 position3 = ((Entity)Game.PlayerPed).Position;
				if (((Vector3)(ref position3)).DistanceToSquared(((Entity)selectedVehicle).Position) > 400f)
				{
					Utils.DisplayHelpText(modelData.IsElectric ? "Your ~b~electric vehicle ~s~has finished charging." : "Your ~b~vehicle ~s~has finished fueling.");
				}
			}
			catch (Exception exception2)
			{
				menuItem.Label = "~r~ERROR";
				Print(exception2);
			}
			finally
			{
				isBusy = false;
			}
		}
		bool IsSelected(string key)
		{
			if (menuItems.ContainsKey(key))
			{
				return menuItems[key] == menuItem;
			}
			return false;
		}
	}

	private async void OnMenuClose(Menu menu, MenuClosedEventArgs e)
	{
		if (menu == menus["pump"])
		{
			Game.PlayerPed.Task.ClearAll();
			await BaseScript.Delay(4000);
			Game.PlayerPed.Task.ClearAllImmediately();
		}
	}

	[Update]
	private async Coroutine ClosestGasStationTask()
	{
		await Script.Wait(1000);
		GasStation gasStation = currentGasStation;
		_ = currentPumpIdx;
		bool flag = canOpenPumpMenu;
		bool flag2 = false;
		currentGasStation = null;
		currentPumpIdx = -1;
		canOpenPumpMenu = false;
		foreach (GasStation gasStation2 in gasStations)
		{
			Vector3 position = ((Entity)Game.PlayerPed).Position;
			float num = ((Vector3)(ref position)).DistanceToSquared(gasStation2.Position);
			if (!(num < 2500f))
			{
				continue;
			}
			currentGasStation = gasStation2;
			int num2 = 0;
			float num3 = 100f;
			Vector4[] array = gasStation2.Pumps.Concat(gasStation2.Chargers).ToArray();
			Vector4[] array2 = array;
			foreach (Vector4 vec in array2)
			{
				position = ((Entity)Game.PlayerPed).Position;
				num = ((Vector3)(ref position)).DistanceToSquared(vec.XYZ());
				if (num < num3)
				{
					num3 = num;
					currentPumpIdx = num2;
				}
				num2++;
			}
			if (currentPumpIdx != -1)
			{
				Vector4 vec2 = array[currentPumpIdx];
				flag2 = currentPumpIdx >= gasStation2.Pumps.Count;
				GasStationsScript gasStationsScript = this;
				position = ((Entity)Game.PlayerPed).Position;
				gasStationsScript.canOpenPumpMenu = ((Vector3)(ref position)).DistanceToSquared(vec2.XYZ()) <= 1f;
			}
			break;
		}
		if (currentGasStation != null && gasStation == null)
		{
			base.Update += DrawGasStationTask;
		}
		else if (currentGasStation == null && gasStation != null)
		{
			base.Update -= DrawGasStationTask;
		}
		if (canOpenPumpMenu && !flag)
		{
			KeysScript.AttachListener((Control)51, OnKeyEvent, 25);
			Utils.AddInstructionalButton("pump", new Gtacnr.Client.API.UI.InstructionalButton(flag2 ? "EV Charger" : "Gas Pump", 2, (Control)51));
		}
		else if (!canOpenPumpMenu && flag)
		{
			KeysScript.DetachListener((Control)51, OnKeyEvent);
			Utils.RemoveInstructionalButton("pump");
		}
	}

	private bool OnKeyEvent(Control control, KeyEventType eventType, InputType inputType)
	{
		if (eventType == KeyEventType.JustPressed && canOpenPumpMenu)
		{
			OpenPumpMenu();
			return true;
		}
		return false;
	}

	private async Coroutine DrawGasStationTask()
	{
		if (currentGasStation != null && currentPumpIdx >= 0)
		{
			float z = 0f;
			Vector4 vec = currentGasStation.Pumps.Concat(currentGasStation.Chargers).ToArray()[currentPumpIdx];
			bool flag = currentPumpIdx >= currentGasStation.Pumps.Count;
			Vector3 val = vec.XYZ();
			if (API.GetGroundZFor_3dCoord(val.X, val.Y, val.Z, ref z, false))
			{
				val.Z = z;
			}
			World.DrawMarker((MarkerType)1, val, Vector3.Zero, Vector3.Zero, new Vector3(0.5f, 0.5f, 0.4f), System.Drawing.Color.FromArgb(flag ? (-2146325982) : (-2136299008)), false, false, false, (string)null, (string)null, false);
		}
	}
}
