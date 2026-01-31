using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using CitizenFX.Core;
using Gtacnr.Model.Enums;
using Newtonsoft.Json;

namespace Gtacnr.Model;

public class Business
{
	public List<BusinessEmployee> Employees = new List<BusinessEmployee>();

	public List<BusinessSupply> Supplies = new List<BusinessSupply>();

	public Dictionary<string, Vector3> LocationResourceOverrides = new Dictionary<string, Vector3>();

	private DateTime AssaultedDateTime;

	public Dictionary<string, Dealership> DealershipResourceOverrides = new Dictionary<string, Dealership>();

	public string Id { get; set; }

	public BusinessType Type { get; set; }

	public string MenuOverride { get; set; }

	public string Name { get; set; }

	public string CompanyName { get; set; }

	public string CreationUserId { get; set; }

	public DateTime CreationDateTime { get; set; }

	public string OwnerCharacterId { get; set; }

	public DateTime? AcquireDateTime { get; set; }

	public int RequiredLevel { get; set; }

	public bool IsIllegal { get; set; }

	public bool IsPoliceOnly { get; set; }

	public ulong Value { get; set; }

	public int Price { get; set; }

	public int CashRegister { get; set; }

	public int Safe { get; set; }

	public bool AcceptsCash { get; set; } = true;

	public bool AcceptsCards { get; set; }

	public float MaxHeight { get; set; } = 6f;

	public bool LimitedStock { get; set; }

	public string MenuHeaderDictionary { get; set; }

	public string MenuHeaderTexture { get; set; }

	public BusinessSafe SafeData { get; set; }

	public BusinessPropPreviewData PropPreviewData { get; set; }

	public BusinessClothingPreviewData ClothingPreviewData { get; set; }

	public List<float[]> RobberyLootLocations { get; set; }

	public List<Vector4> RobberyLootCoords => RobberyLootLocations?.Select((Func<float[], Vector4>)((float[] f4) => new Vector4(f4[0], f4[1], f4[2], f4[3]))).ToList();

	public float PriceMultiplier { get; set; } = 1f;

	public Dictionary<string, float> PriceMultipliers { get; set; } = new Dictionary<string, float>();

	public float DemandPayoutMultiplier { get; set; } = 1f;

	public float RobberyAmountMultiplier { get; set; } = 1f;

	public int RobberyMinCops { get; set; }

	[JsonProperty("Location_")]
	public Vector3 Location { get; set; }

	public bool IsBeingRobbed { get; set; }

	public bool CopsCalled { get; set; }

	public DateTime ShopliftDateTime { get; set; }

	public bool EmployeesAssaulted => !Utils.CheckTimePassed(AssaultedDateTime, TimeSpan.FromSeconds(60.0));

	public Blip Blip { get; set; }

	public bool? ShowBlip { get; set; }

	public List<string> BlipJobs { get; set; }

	public int? BlipSprite { get; set; }

	public int? BlipColor { get; set; }

	public float? BlipScale { get; set; }

	public float[] BlipCoords_ { get; set; }

	public Vector3 BlipCoords
	{
		get
		{
			//IL_002a: Unknown result type (might be due to invalid IL or missing references)
			//IL_000a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0010: Unknown result type (might be due to invalid IL or missing references)
			if (BlipCoords_ == null)
			{
				return default(Vector3);
			}
			return new Vector3(BlipCoords_[0], BlipCoords_[1], BlipCoords_[2]);
		}
	}

	public bool BlipOnlyOnMainMap { get; set; }

	public bool BlipOnlyOnMinimap { get; set; }

	public string? RequiredResource { get; set; }

	public BusinessJewelryRobberyData JewelryRobbery { get; set; }

	public Dealership Dealership { get; set; }

	public MechanicShop Mechanic { get; set; }

	public PoliceStation PoliceStation { get; set; }

	public Hospital Hospital { get; set; }

	public AirportData Airport { get; set; }

	public float GetMultiplier(string key)
	{
		if (!PriceMultipliers.ContainsKey(key))
		{
			if (!PriceMultipliers.ContainsKey(""))
			{
				return 1f;
			}
			return PriceMultipliers[""];
		}
		return PriceMultipliers[key];
	}

	public void AssaultEmployees()
	{
		AssaultedDateTime = DateTime.UtcNow;
	}

	public void ResetRobberyState()
	{
		IsBeingRobbed = false;
		AssaultedDateTime = default(DateTime);
		CopsCalled = false;
	}

	public bool HasRequiredResource()
	{
		if (string.IsNullOrWhiteSpace(RequiredResource))
		{
			return true;
		}
		if (RequiredResource.StartsWith("!"))
		{
			return !Utils.IsResourceLoadedOrLoading(RequiredResource.Substring(1));
		}
		return Utils.IsResourceLoadedOrLoading(RequiredResource);
	}

	[OnDeserialized]
	internal void OnDeserializedMethod(StreamingContext context)
	{
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_0100: Unknown result type (might be due to invalid IL or missing references)
		//IL_0107: Unknown result type (might be due to invalid IL or missing references)
		//IL_010d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_012a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0131: Unknown result type (might be due to invalid IL or missing references)
		//IL_0137: Unknown result type (might be due to invalid IL or missing references)
		//IL_011e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0148: Unknown result type (might be due to invalid IL or missing references)
		foreach (string key in LocationResourceOverrides.Keys)
		{
			Vector3 location = LocationResourceOverrides[key];
			if (Utils.IsResourceLoadedOrLoading(key))
			{
				Location = location;
			}
		}
		foreach (string key2 in DealershipResourceOverrides.Keys)
		{
			Dealership dealership = DealershipResourceOverrides[key2];
			if (Utils.IsResourceLoadedOrLoading(key2))
			{
				if (dealership.Cameras.Count != 0)
				{
					Dealership.Cameras = dealership.Cameras;
				}
				if (dealership.CarPosition != default(Vector4))
				{
					Dealership.CarPosition = dealership.CarPosition;
				}
				if (dealership.CarOutPosition != default(Vector4))
				{
					Dealership.CarOutPosition = dealership.CarOutPosition;
				}
				if (dealership.PlayerLookPosition != default(Vector4))
				{
					Dealership.PlayerLookPosition = dealership.PlayerLookPosition;
				}
				if (dealership.DealerLookPosition != default(Vector4))
				{
					Dealership.DealerLookPosition = dealership.DealerLookPosition;
				}
			}
		}
	}

	public override string ToString()
	{
		if (string.IsNullOrWhiteSpace(CompanyName))
		{
			return Name;
		}
		return CompanyName + " (" + Name + ")";
	}
}
