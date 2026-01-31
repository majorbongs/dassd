using System;
using System.Collections.Generic;
using System.Linq;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Gtacnr.Client.API.Scripts;
using Gtacnr.Client.API.UI;
using Gtacnr.Client.HUD;
using Gtacnr.Client.Vehicles.Fuel;
using Gtacnr.Model;
using MenuAPI;
using NativeUI;

namespace Gtacnr.Client.Businesses.Airports;

public class AircraftStandScript : Script
{
	private List<AircraftStand> aircraftStands = Gtacnr.Utils.LoadJson<List<AircraftStand>>("data/vehicles/aircraftStands.json");

	private List<Model> jetModels = (from i in Gtacnr.Utils.LoadJson<List<string>>("data/vehicles/jets.json")
		select Model.op_Implicit(Game.GenerateHash(i))).ToList();

	private AircraftStand? targetStand;

	private Vehicle? targetVehicle;

	private float selectedRefuelAmountGal;

	private bool isBusy;

	private bool controlsEnabled;

	private Menu? menu;

	private Dictionary<string, MenuItem> menuItems = new Dictionary<string, MenuItem>();

	private readonly float STAND_RADIUS = 10f;

	protected override void OnStarted()
	{
		menu = new Menu("Ground Services", "Choose an option")
		{
			MaxDistance = 20f
		};
		menu.OnSliderPositionChange += OnMenuSliderPositionChange;
		menu.OnSliderItemSelect += OnMenuSliderItemSelect;
		MenuController.AddMenu(menu);
	}

	[Update]
	private async Coroutine CheckTask()
	{
		await Script.Wait(2000);
		Vehicle currentVehicle = Game.PlayerPed.CurrentVehicle;
		if ((Entity)(object)currentVehicle == (Entity)null || !currentVehicle.Exists() || (!((Entity)currentVehicle).Model.IsPlane && !((Entity)currentVehicle).Model.IsHelicopter) || ((Entity)currentVehicle).IsInAir || (Entity)(object)currentVehicle.Driver != (Entity)(object)Game.PlayerPed)
		{
			return;
		}
		bool flag = false;
		float num = 10000f;
		foreach (AircraftStand aircraftStand in aircraftStands)
		{
			Vector3 position = ((Entity)currentVehicle).Position;
			float num2 = ((Vector3)(ref position)).DistanceToSquared2D(aircraftStand.Position);
			if (num2 < num)
			{
				flag = true;
				num = num2;
				targetStand = aircraftStand;
			}
		}
		if (!flag)
		{
			targetStand = null;
		}
	}

	[Update]
	private async Coroutine ControlTask()
	{
		await Script.Wait(200);
		Vehicle currentVehicle = Game.PlayerPed.CurrentVehicle;
		if (targetStand == null || !Game.PlayerPed.IsInVehicle() || currentVehicle.Speed > 0.2f)
		{
			DisableControls();
			return;
		}
		if (targetStand == null)
		{
			DisableControls();
			return;
		}
		Vector3 position = ((Entity)currentVehicle).Position;
		if (((Vector3)(ref position)).DistanceToSquared2D(targetStand.Position) > STAND_RADIUS * STAND_RADIUS)
		{
			DisableControls();
		}
		else
		{
			EnableControls();
		}
	}

	private void EnableControls()
	{
		if (!controlsEnabled)
		{
			controlsEnabled = true;
			Utils.AddInstructionalButton("grdSvc", new Gtacnr.Client.API.UI.InstructionalButton("Ground Services", 2, (Control)113));
			KeysScript.AttachListener((Control)113, OnKeyEvent, 15);
		}
	}

	private void DisableControls()
	{
		if (controlsEnabled)
		{
			controlsEnabled = false;
			Utils.RemoveInstructionalButton("grdSvc");
			KeysScript.DetachListener((Control)113, OnKeyEvent);
		}
	}

	private bool OnKeyEvent(Control control, KeyEventType eventType, InputType inputType)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0003: Invalid comparison between Unknown and I4
		if ((int)control == 113 && eventType == KeyEventType.JustPressed)
		{
			if (targetStand == null || MenuController.IsAnyMenuOpen())
			{
				return false;
			}
			OpenMenu();
			return true;
		}
		return false;
	}

	private void OpenMenu()
	{
		menu?.OpenMenu();
		RefreshMenu();
	}

	private void RefreshMenu()
	{
		if (menu == null || targetStand == null)
		{
			return;
		}
		try
		{
			menu.MenuSubtitle = targetStand.Name;
			menu.ClearMenuItems();
			menu.AddLoadingMenuItem();
			Vehicle val = Game.PlayerPed.CurrentVehicle;
			if ((Entity)(object)val == (Entity)null || !val.Exists())
			{
				val = Game.PlayerPed.LastVehicle;
			}
			if ((Entity)(object)val == (Entity)null || (!((Entity)val).Model.IsPlane && !((Entity)val).Model.IsHelicopter))
			{
				menu.ClearMenuItems();
				menu.AddMenuItem(new MenuItem("No options :(", "You need an aircraft to use this menu."));
				return;
			}
			targetVehicle = val;
			menu.ClearMenuItems();
			if (targetStand.Services.Contains("Fuel"))
			{
				GetAircraftFuelBasePrice(targetVehicle);
				_ = targetStand.PriceMultiplier;
				Utils.GetVehicleRefuelAmount(targetVehicle);
				Menu? obj = menu;
				Dictionary<string, MenuItem> dictionary = menuItems;
				MenuSliderItem obj2 = new MenuSliderItem("Refuel", 1, 20, 1)
				{
					PlaySelectSound = false
				};
				MenuItem item = obj2;
				dictionary["fuel"] = obj2;
				obj.AddMenuItem(item);
				menuItems["fuel"].Description = ComputeFuelMenuItemDescription(1);
			}
		}
		catch (Exception e)
		{
			menu.ClearMenuItems();
			menu.AddErrorMenuItem(e);
		}
	}

	private string? GetAircraftFuelType(Vehicle vehicle)
	{
		if (!jetModels.Contains(((Entity)vehicle).Model))
		{
			if (!((Entity)vehicle).Model.IsPlane)
			{
				if (!((Entity)vehicle).Model.IsHelicopter)
				{
					return null;
				}
				return "Helicopters";
			}
			return "Airplanes";
		}
		return "Jets";
	}

	private float GetAircraftFuelBasePrice(Vehicle vehicle)
	{
		return GasStationsScript.GasPrices[GetAircraftFuelType(vehicle) ?? ""];
	}

	private string ComputeFuelMenuItemDescription(int sliderPosition)
	{
		if ((Entity)(object)targetVehicle == (Entity)null || targetStand == null)
		{
			return "~r~ERROR";
		}
		float num = GetAircraftFuelBasePrice(targetVehicle) * targetStand.PriceMultiplier;
		float vehicleRefuelAmount = Utils.GetVehicleRefuelAmount(targetVehicle);
		selectedRefuelAmountGal = ((vehicleRefuelAmount == 0f) ? 0f : (vehicleRefuelAmount * ((float)sliderPosition * 0.05f)));
		int amount = (selectedRefuelAmountGal * num).ToIntCeil();
		return $"~g~{amount.ToCurrencyString()} ~s~for ~b~{selectedRefuelAmountGal:0.##} ~s~gallons (${num:0.##}/gal). ~y~Cashless payments only.";
	}

	private void OnMenuSliderPositionChange(Menu menu, MenuSliderItem sliderItem, int oldPosition, int newPosition, int itemIndex)
	{
		if (IsSelected("fuel") && (Entity)(object)targetVehicle != (Entity)null && targetVehicle.Exists() && targetStand != null)
		{
			sliderItem.Description = ComputeFuelMenuItemDescription(newPosition);
		}
		bool IsSelected(string key)
		{
			if (menuItems.ContainsKey(key))
			{
				return menuItems[key] == sliderItem;
			}
			return false;
		}
	}

	private async void OnMenuSliderItemSelect(Menu menu, MenuSliderItem menuItem, int sliderPosition, int itemIndex)
	{
		if (!IsSelected("fuel"))
		{
			return;
		}
		if (isBusy || selectedRefuelAmountGal <= 0f || (Entity)(object)targetVehicle == (Entity)null || targetStand == null)
		{
			Utils.PlayErrorSound();
			return;
		}
		try
		{
			isBusy = true;
			Utils.PlaySelectSound();
			API.SetVehicleJetEngineOn(((PoolObject)targetVehicle).Handle, false);
			targetVehicle.IsEngineRunning = false;
			menuItem.Enabled = false;
			float max = Utils.GetVehicleTankCapacity(targetVehicle);
			float targetAmount = Math.Min(Utils.GetVehicleFuel(targetVehicle) + selectedRefuelAmountGal, max);
			int num = await TriggerServerEventAsync<int>("gtacnr:fuel:buy", new object[3]
			{
				1000 + aircraftStands.IndexOf(targetStand),
				selectedRefuelAmountGal,
				GetAircraftFuelType(targetVehicle) ?? ""
			});
			if (num != 1)
			{
				MenuController.CloseAllMenus();
				Utils.DisplayErrorMessage(36, num);
				return;
			}
			float currentAmount = targetAmount - selectedRefuelAmountGal;
			float percentage = currentAmount / max;
			float totalAddedGal = 0f;
			BarTimerBar bar = null;
			try
			{
				bar = new BarTimerBar("FUEL")
				{
					Percentage = percentage
				};
				TimerBarScript.AddTimerBar(bar);
				while (currentAmount < targetAmount)
				{
					await BaseScript.Delay(100);
					Vector3 position = ((Entity)targetVehicle).Position;
					if (!(((Vector3)(ref position)).DistanceToSquared(targetStand.Position) > STAND_RADIUS * STAND_RADIUS))
					{
						float num2 = 0.2f;
						currentAmount += num2;
						totalAddedGal += num2;
						if (currentAmount > targetAmount)
						{
							currentAmount = targetAmount;
						}
						bar.Percentage = currentAmount / max;
						continue;
					}
					break;
				}
			}
			catch (Exception exception)
			{
				Print(exception);
			}
			finally
			{
				if (bar != null)
				{
					TimerBarScript.RemoveTimerBar(bar);
				}
			}
			if (currentAmount < targetAmount)
			{
				menuItem.Label = "CANCELED";
			}
			else
			{
				menuItem.Label = "FULL";
			}
			float vehicleTankCapacityL = Utils.GetVehicleTankCapacityL(targetVehicle);
			string text = Utils.GetVehicleHealthData(targetVehicle)?.Json();
			BaseScript.TriggerServerEvent("gtacnr:fuel:completed", new object[4]
			{
				totalAddedGal,
				((Entity)targetVehicle).NetworkId,
				vehicleTankCapacityL,
				text
			});
		}
		catch (Exception exception2)
		{
			Print(exception2);
		}
		finally
		{
			isBusy = false;
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
}
