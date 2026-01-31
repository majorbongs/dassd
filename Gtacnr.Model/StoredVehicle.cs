using System;
using System.Linq;
using Gtacnr.Client;
using Gtacnr.Client.Businesses.Dealerships;
using Gtacnr.Client.Vehicles;
using Gtacnr.Localization;
using Gtacnr.Model.Enums;
using MenuAPI;

namespace Gtacnr.Model;

public class StoredVehicle
{
	public string Id { get; set; }

	public string OwnerCharacterId { get; set; }

	public int Model { get; set; }

	public string LicensePlate { get; set; }

	public bool IsStolen { get; set; }

	public bool IsDead { get; set; }

	public bool IsImpounded { get; set; }

	public bool IsInMaintenance { get; set; }

	public string Job { get; set; }

	public string GarageId { get; set; }

	public int GarageParkIndex { get; set; }

	[Obsolete]
	public float Fuel { get; set; } = 1f;

	public VehicleModData ModData { get; set; } = new VehicleModData();

	public VehicleHealthData HealthData { get; set; } = new VehicleHealthData();

	public VehicleOwnershipData OwnershipData { get; set; } = new VehicleOwnershipData();

	public VehicleRentData RentData { get; set; }

	public int NetworkId { get; set; }

	public MenuItem ToMenuItem()
	{
		string vehicleFullName = Gtacnr.Client.Utils.GetVehicleFullName(Model);
		bool flag = Id == ActiveVehicleScript.ActiveVehicleStoredId;
		string text = "undefined";
		if (ModData != null && DealershipScript.VehicleColors.TryGetValue(ModData.PrimaryColor, out VehicleColorInfo value))
		{
			text = value.Description;
		}
		VehicleHealthData vehicleHealthData = (flag ? ActiveVehicleScript.ActiveVehicleHealthData : HealthData);
		float num = vehicleHealthData?.EngineHealth ?? 0f;
		_ = vehicleHealthData?.BodyHealth;
		float num2 = vehicleHealthData?.PetrolTankHealth ?? 0f;
		int? num3 = vehicleHealthData?.WheelHealth?.Where((float t) => t < 1f).Count();
		string text2 = $"{num / 10f:0}%";
		string text3 = ((num2 < 950f) ? LocalizationController.S(Entries.Vehicles.MENU_VEHICLES_TANK_DAMAGED) : LocalizationController.S(Entries.Vehicles.MENU_VEHICLES_OK));
		string text4 = ((num3 == 0 || !num3.HasValue) ? LocalizationController.S(Entries.Vehicles.MENU_VEHICLES_OK) : LocalizationController.S(Entries.Vehicles.MENU_VEHICLES_TIRE_FLAT, num3));
		bool flag2 = (vehicleHealthData != null && vehicleHealthData.EngineHealth < 500f) || (vehicleHealthData != null && vehicleHealthData.PetrolTankHealth < 500f);
		PersonalVehicleModel personalVehicleModel = DealershipScript.FindVehicleModelData(Model);
		bool flag3 = personalVehicleModel?.WasRecalled ?? true;
		if (HealthData != null && HealthData.Fuel > 1f)
		{
			HealthData.Fuel = 1f;
		}
		float num4 = (flag ? ActiveVehicleScript.ActiveVehicleHealthData.Fuel : (HealthData?.Fuel ?? 0f));
		string text5 = $"{num4 * 100f:0}%";
		bool flag4 = num4 <= 0.15f;
		string text6 = (IsDead ? "~r~" : (flag3 ? " ~r~" : (IsImpounded ? " ~y~" : (IsInMaintenance ? "~y~" : (flag2 ? "~o~" : (flag4 ? "~o~" : ""))))));
		MenuItem menuItem = new MenuItem(flag ? ("~b~" + vehicleFullName) : (text6 + vehicleFullName));
		bool flag5 = !IsDead && !IsImpounded && !IsInMaintenance && !flag3;
		bool flag6 = flag5 && (personalVehicleModel == null || personalVehicleModel.Type != PersonalVehicleType.Bicycle);
		bool flag7 = flag6;
		menuItem.Description = (flag ? ("~g~" + LocalizationController.S(Entries.Vehicles.MENU_VEHICLES_ACTIVE) + "~n~") : "") + (IsDead ? ("~r~" + LocalizationController.S(Entries.Vehicles.MENU_VEHICLES_DESTROYED) + "~n~") : "") + (IsImpounded ? ("~y~" + LocalizationController.S(Entries.Vehicles.MENU_VEHICLES_IMPOUNDED) + "~n~") : "") + (IsInMaintenance ? ("~y~" + LocalizationController.S(Entries.Vehicles.MENU_VEHICLES_IN_MAINTENANCE) + "~n~") : "") + (flag2 ? ("~o~" + LocalizationController.S(Entries.Vehicles.MENU_VEHICLES_DAMAGED) + "~n~") : "") + (flag4 ? ("~o~" + ((personalVehicleModel != null && personalVehicleModel.IsElectric) ? LocalizationController.S(Entries.Vehicles.MENU_VEHICLES_BATTERY_LOW) : LocalizationController.S(Entries.Vehicles.MENU_VEHICLES_FUEL_LOW)) + "~n~") : "") + (flag3 ? ("~o~" + LocalizationController.S(Entries.Vehicles.MENU_VEHICLES_RECALLED) + "~n~") : "") + "~b~" + LocalizationController.S(Entries.Vehicles.MENU_VEHICLES_COLOR) + ": ~s~" + text + (flag7 ? ("~n~~b~" + LocalizationController.S(Entries.Vehicles.MENU_VEHICLES_PLATE) + ": ~s~" + LicensePlate) : "") + (flag5 ? ("~n~~b~" + LocalizationController.S(Entries.Vehicles.MENU_VEHICLES_HEALTH) + ": ~s~" + text2) : "") + (flag6 ? (" ~b~" + ((personalVehicleModel != null && personalVehicleModel.IsElectric) ? LocalizationController.S(Entries.Vehicles.MENU_VEHICLES_BATTERY) : LocalizationController.S(Entries.Vehicles.MENU_VEHICLES_FUEL)) + ": ~s~" + text5) : "") + (flag5 ? ("~n~~b~" + LocalizationController.S(Entries.Vehicles.MENU_VEHICLES_TANK) + ": ~s~" + text3) : "") + (flag5 ? (" ~b~" + LocalizationController.S(Entries.Vehicles.MENU_VEHICLES_TIRES) + ": ~s~" + text4) : "");
		menuItem.ItemData = this;
		menuItem.Label = text6 + LicensePlate;
		return menuItem;
	}
}
