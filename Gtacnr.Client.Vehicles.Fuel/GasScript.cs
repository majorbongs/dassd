using System;
using System.Collections.Generic;
using System.Linq;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Gtacnr.Client.API;
using Gtacnr.Client.API.Scripts;
using Gtacnr.Client.Businesses.Dealerships;
using Gtacnr.Client.HUD;
using Gtacnr.Localization;
using Gtacnr.Model;
using Gtacnr.Model.Enums;
using NativeUI;

namespace Gtacnr.Client.Vehicles.Fuel;

public class GasScript : Script
{
	public static readonly float GALLONS_PER_LITER = 0.264172f;

	public static readonly float LITERS_PER_GALLON = 3.78541f;

	public static readonly float JERRYCAN_CAPACITY = 5f;

	private static GasConfig config = Gtacnr.Utils.LoadJson<GasConfig>("data/vehicles/gasConfig.json");

	private static Dictionary<int, float> capacityOverrides = Gtacnr.Utils.LoadJson<Dictionary<string, float>>("data/vehicles/gasCapacityOverrides.json").ToDictionary((KeyValuePair<string, float> kvp) => Game.GenerateHash(kvp.Key), (KeyValuePair<string, float> kvp) => kvp.Value);

	private BarTimerBar fuelLevelBar;

	private bool isNavigateListenerAttached;

	public static bool IsFuelPromptEnabled = Preferences.LowFuelPrompt.Get();

	public static bool AlwaysShowFuelBar = Preferences.AlwaysShowFuelBar.Get();

	public static float OverrideFuelLevel = -1f;

	public static IReadOnlyDictionary<int, float> CapacityOverrides => capacityOverrides;

	public GasScript()
	{
		VehicleEvents.EnteredVehicle += OnEnteredVehicle;
	}

	private async void OnEnteredVehicle(object sender, VehicleEventArgs e)
	{
		if ((int)e.Seat != -1)
		{
			RemoveFuelBar();
			return;
		}
		if (IsClassIgnored(e.Vehicle.ClassType))
		{
			RemoveFuelBar();
			return;
		}
		if (IsModelIgnored(((Entity)e.Vehicle).Model))
		{
			RemoveFuelBar();
			return;
		}
		float currentVehicleFuelPercent = -1f;
		float consumedFuelPercentSinceUpdate = 0f;
		DateTime lastFuelConsumptionUpdate = DateTime.UtcNow;
		OverrideFuelLevel = -1f;
		while ((Entity)(object)Game.PlayerPed.CurrentVehicle == (Entity)(object)e.Vehicle)
		{
			await BaseScript.Delay(500);
			if (!e.Vehicle.IsEngineRunning)
			{
				RemoveFuelBar();
				continue;
			}
			string key = (((int)e.Vehicle.ClassType == 16) ? "Airplanes" : (((int)e.Vehicle.ClassType == 15) ? "Helicopters" : (((int)e.Vehicle.ClassType == 14) ? "Boats" : "LandVehicles")));
			if (OverrideFuelLevel != -1f)
			{
				currentVehicleFuelPercent = OverrideFuelLevel;
				OverrideFuelLevel = -1f;
			}
			if (currentVehicleFuelPercent == -1f)
			{
				VehicleState vehicleState = LatentVehicleStateScript.Get(((Entity)e.Vehicle).NetworkId);
				if (vehicleState != null && vehicleState.Fuel != -1f)
				{
					currentVehicleFuelPercent = vehicleState.Fuel;
				}
			}
			if (currentVehicleFuelPercent == -1f)
			{
				ConsumptionData consumptionData = config.Behavior.Modifiers[key];
				currentVehicleFuelPercent = (float)Gtacnr.Utils.GetRandomDouble(consumptionData.MinStartValue, consumptionData.MaxStartValue);
				InitializeGas(e.Vehicle, currentVehicleFuelPercent);
			}
			if (currentVehicleFuelPercent < 0f)
			{
				currentVehicleFuelPercent = 0f;
			}
			else if (currentVehicleFuelPercent > 1f)
			{
				currentVehicleFuelPercent = 1f;
			}
			float vehicleTankCapacityL = Utils.GetVehicleTankCapacityL(e.Vehicle);
			float num = vehicleTankCapacityL * currentVehicleFuelPercent;
			float rPMImpact = config.Behavior.Modifiers[key].RPMImpact;
			float accelerationImpact = config.Behavior.Modifiers[key].AccelerationImpact;
			float tractionImpact = config.Behavior.Modifiers[key].TractionImpact;
			float multiplier = config.Behavior.Modifiers[key].Multiplier;
			float num2 = 0f;
			num2 += (float)Math.Pow(e.Vehicle.CurrentRPM, 1.5) * rPMImpact;
			num2 += e.Vehicle.Acceleration * accelerationImpact;
			num2 += e.Vehicle.MaxTraction * tractionImpact;
			num2 *= multiplier;
			num -= num2;
			if (num < 0f)
			{
				num = 0f;
			}
			currentVehicleFuelPercent = num / vehicleTankCapacityL;
			float num3 = num2 / vehicleTankCapacityL;
			if (currentVehicleFuelPercent >= 0.1f || currentVehicleFuelPercent <= 0.01f)
			{
				e.Vehicle.FuelLevel = num;
			}
			else
			{
				e.Vehicle.FuelLevel = vehicleTankCapacityL * 0.11f;
			}
			consumedFuelPercentSinceUpdate += num3;
			if (Gtacnr.Utils.CheckTimePassed(lastFuelConsumptionUpdate, 5000.0))
			{
				BaseScript.TriggerServerEvent("gtacnr:fuel:consume", new object[2]
				{
					((Entity)e.Vehicle).NetworkId,
					consumedFuelPercentSinceUpdate
				});
				consumedFuelPercentSinceUpdate = 0f;
				lastFuelConsumptionUpdate = DateTime.UtcNow;
			}
			PersonalVehicleModel modelData;
			if (currentVehicleFuelPercent <= 0.25f || AlwaysShowFuelBar)
			{
				if (fuelLevelBar == null)
				{
					modelData = DealershipScript.FindVehicleModelData(Model.op_Implicit(((Entity)e.Vehicle).Model));
					fuelLevelBar = new BarTimerBar((modelData?.IsElectric ?? false) ? LocalizationController.S(Entries.Vehicles.BATTERY_BAR) : LocalizationController.S(Entries.Vehicles.FUEL_BAR));
					TimerBarScript.AddTimerBar(fuelLevelBar);
					ShowPrompt();
				}
				fuelLevelBar.Color = ((currentVehicleFuelPercent < 0.15f) ? BarColors.Red : BarColors.Orange);
				fuelLevelBar.TextColor = ((currentVehicleFuelPercent < 0.15f) ? TextColors.Red : TextColors.Orange);
				fuelLevelBar.Percentage = currentVehicleFuelPercent;
			}
			else
			{
				RemoveFuelBar();
			}
			if (currentVehicleFuelPercent <= 0.005f && e.Vehicle.IsEngineRunning)
			{
				e.Vehicle.IsEngineRunning = false;
			}
			async void ShowPrompt()
			{
				await BaseScript.Delay(2000);
				if ((Entity)(object)Game.PlayerPed.CurrentVehicle != (Entity)null && IsFuelPromptEnabled && currentVehicleFuelPercent <= 0.25f)
				{
					await InteractiveNotificationsScript.Show(LocalizationController.S(Entries.Vehicles.RUNNING_LOW_FUEL, (modelData?.IsElectric ?? false) ? LocalizationController.S(Entries.Vehicles.RUNNING_LOW_BATTERY_UNIT) : LocalizationController.S(Entries.Vehicles.RUNNING_LOW_FUEL_UNIT)), InteractiveNotificationType.HelpText, OnAccepted, TimeSpan.FromSeconds(10.0), 0u, "Set GPS", "Set GPS (hold)");
				}
			}
		}
		BaseScript.TriggerServerEvent("gtacnr:fuel:consume", new object[2]
		{
			((Entity)e.Vehicle).NetworkId,
			consumedFuelPercentSinceUpdate
		});
		RemoveFuelBar();
		static bool OnAccepted()
		{
			//IL_0021: Unknown result type (might be due to invalid IL or missing references)
			//IL_0026: Unknown result type (might be due to invalid IL or missing references)
			//IL_002b: Unknown result type (might be due to invalid IL or missing references)
			//IL_005e: Unknown result type (might be due to invalid IL or missing references)
			GasStation gasStation = null;
			float num4 = 999999f;
			foreach (GasStation gasStation2 in GasStationsScript.GasStations)
			{
				Vector3 position = ((Entity)Game.PlayerPed).Position;
				float num5 = ((Vector3)(ref position)).DistanceToSquared(gasStation2.Position);
				if (num5 < num4)
				{
					num4 = num5;
					gasStation = gasStation2;
				}
			}
			if (gasStation != null)
			{
				GPSScript.SetDestination("Gas Station", gasStation.Position, 30f, shortRange: true, null, null, 255, autoDelete: true);
				Utils.DisplayHelpText();
			}
			return true;
		}
	}

	public unsafe static bool IsClassIgnored(VehicleClass classType)
	{
		return config.IgnoredClasses.Contains(((object)(*(VehicleClass*)(&classType))/*cast due to .constrained prefix*/).ToString());
	}

	public static bool IsModelIgnored(Model model)
	{
		return config.IgnoredVehicles.Any((string v) => API.GetHashKey(v) == model.Hash);
	}

	public static void InitializeGas(Vehicle vehicle, float percentage)
	{
		BaseScript.TriggerServerEvent("gtacnr:fuel:initVehicle", new object[2]
		{
			((Entity)vehicle).NetworkId,
			percentage
		});
	}

	private void RemoveFuelBar()
	{
		if (fuelLevelBar != null)
		{
			TimerBarScript.RemoveTimerBar(fuelLevelBar);
			fuelLevelBar = null;
		}
	}
}
