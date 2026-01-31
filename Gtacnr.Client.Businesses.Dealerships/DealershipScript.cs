using System;
using System.Collections.Generic;
using System.Linq;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Gtacnr.Model;
using Gtacnr.Model.Enums;

namespace Gtacnr.Client.Businesses.Dealerships;

public class DealershipScript : Script
{
	private bool canOpenDealershipMenu;

	private bool instructionsShown;

	public static Dictionary<string, VehicleMakeInfo> VehicleMakes { get; private set; }

	public static Dictionary<string, PersonalVehicleModel> VehicleModelData { get; private set; } = new Dictionary<string, PersonalVehicleModel>();

	public static Dictionary<string, BusinessTypeMetadata> DealershipTypes { get; private set; } = Gtacnr.Utils.LoadJson<Dictionary<string, BusinessTypeMetadata>>("data/vehicles/dealershipTypes.json");

	public static Dictionary<DealershipType, List<DealershipSupply>> DealershipSupplies { get; private set; } = Gtacnr.Utils.LoadJson<Dictionary<DealershipType, List<DealershipSupply>>>("data/vehicles/dealershipSupplies.json");

	public static Dictionary<int, VehicleColorInfo> VehicleColors { get; private set; } = Gtacnr.Utils.LoadJson<IEnumerable<VehicleColorInfo>>("data/vehicles/vehicleColors.json").ToDictionary((VehicleColorInfo x) => x.Id, (VehicleColorInfo x) => x);

	public static IEnumerable<Business> Dealerships => BusinessScript.Businesses.Values.Where((Business b) => b.Type == BusinessType.Dealership && b.Dealership != null);

	public static bool IsInDealership { get; set; }

	public static bool WasDataLoaded { get; private set; }

	public DealershipScript()
	{
		LoadVehicleMakes();
		LoadVehicleModelData();
		WasDataLoaded = true;
	}

	public static Dealership GetDealershipById(string businessId)
	{
		if (!BusinessScript.Businesses.ContainsKey(businessId))
		{
			return null;
		}
		return BusinessScript.Businesses[businessId].Dealership;
	}

	public static Dealership GetClosestDealership()
	{
		return BusinessScript.Businesses.Values.Where((Business b) => b.Dealership != null).OrderBy(delegate(Business b)
		{
			//IL_0005: Unknown result type (might be due to invalid IL or missing references)
			//IL_000a: Unknown result type (might be due to invalid IL or missing references)
			//IL_000e: Unknown result type (might be due to invalid IL or missing references)
			Vector3 position = ((Entity)Game.PlayerPed).Position;
			return ((Vector3)(ref position)).DistanceToSquared2D(b.Location);
		}).First()
			.Dealership;
	}

	private void LoadVehicleMakes()
	{
		try
		{
			VehicleMakes = Gtacnr.Utils.LoadJson<Dictionary<string, VehicleMakeInfo>>("data/vehicles/vehicleMakes.json");
		}
		catch (Exception exception)
		{
			Print("^1ERROR: An unexpected error has occurred while parsing vehicle makes.");
			Print(exception);
		}
	}

	private void LoadVehicleModelData()
	{
		try
		{
			foreach (string item in Gtacnr.Utils.LoadJson<List<string>>("gtacnr_items", "data/vehicles/files.json"))
			{
				try
				{
					Dictionary<string, PersonalVehicleModel> dictionary = Gtacnr.Utils.LoadJson<List<PersonalVehicleModel>>("gtacnr_items", "data/vehicles/" + item).ToDictionary((PersonalVehicleModel x) => x.Id, (PersonalVehicleModel x) => x);
					foreach (KeyValuePair<string, PersonalVehicleModel> item2 in dictionary)
					{
						VehicleModelData[item2.Key] = item2.Value;
					}
					Print($"Loaded {dictionary.Count} vehicles from file {item}.");
				}
				catch (Exception ex)
				{
					Print("^1ERROR: Unable to load vehicle model file " + item + ". Reason: " + ex.Message);
				}
			}
		}
		catch (Exception exception)
		{
			Print("^1ERROR: An unexpected error has occurred while parsing vehicle model data.");
			Print(exception);
		}
	}

	public static PersonalVehicleModel FindVehicleModelData(int modelHash)
	{
		foreach (string key in VehicleModelData.Keys)
		{
			if (API.GetHashKey(key) == modelHash)
			{
				return VehicleModelData[key];
			}
		}
		return null;
	}

	[EventHandler("gtacnr:serverDateChanged")]
	private void OnServerDateChanged(string dateTimeS)
	{
		if (MainScript.ServerDateTime > DateTime.MinValue)
		{
			RefreshDealershipDiscounts();
		}
	}

	private void RefreshDealershipDiscounts()
	{
		try
		{
			float tax = ShoppingScript.GetTax(BusinessSupplyType.Vehicle);
			foreach (Business item in BusinessScript.Businesses.Values.Where((Business b) => b.Dealership != null))
			{
				Dealership dealership = item.Dealership;
				if (!DealershipSupplies.ContainsKey(dealership.Type))
				{
					continue;
				}
				foreach (DealershipSupply item2 in DealershipSupplies[dealership.Type])
				{
					item2.SalesTax = tax;
					if (VehicleModelData.ContainsKey(item2.Vehicle))
					{
						item2.ModelData = VehicleModelData[item2.Vehicle];
						if (item2.ModelData.Discounts.Count > 0)
						{
							List<PersonalVehicleModelDiscount> list = item2.ModelData.Discounts.ToList();
							item2.ModelData.Discounts.Clear();
							foreach (PersonalVehicleModelDiscount item3 in list)
							{
								if (item3.StartDate.Date <= DateTime.UtcNow && DateTime.UtcNow <= item3.EndDate.Date)
								{
									item2.ApplyDiscount(item3.PercentOff);
									item2.ModelData.Discounts.Add(item3);
								}
							}
						}
					}
					dealership.Supplies.Add(item2);
				}
			}
			foreach (List<DealershipSupply> value in DealershipSupplies.Values)
			{
				foreach (DealershipSupply item4 in value.Where((DealershipSupply s) => s.ModelData == null))
				{
					if (VehicleModelData.ContainsKey(item4.Vehicle))
					{
						item4.ModelData = VehicleModelData[item4.Vehicle];
					}
				}
			}
		}
		catch (Exception exception)
		{
			Print("^1ERROR: An unexpected error has occurred while parsing discounts.");
			Print(exception);
		}
	}

	public static DealershipSupply FindFirstSupplyOfModel(int modelHash)
	{
		foreach (List<DealershipSupply> value in DealershipSupplies.Values)
		{
			foreach (DealershipSupply item in value)
			{
				if (API.GetHashKey(item.Vehicle) == modelHash)
				{
					return item;
				}
			}
		}
		return null;
	}

	public static Model GetModelFromString(string modelString)
	{
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Expected O, but got Unknown
		Random random = new Random();
		if (modelString.Contains("|"))
		{
			string[] array = modelString.Split('|');
			int num = random.Next(array.Length);
			modelString = array[num];
		}
		return new Model(modelString);
	}
}
