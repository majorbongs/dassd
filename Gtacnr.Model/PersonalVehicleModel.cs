using System;
using System.Collections.Generic;
using Gtacnr.Model.Enums;

namespace Gtacnr.Model;

public class PersonalVehicleModel : IExtraDataContainer, IEconomyItem
{
	public string Id { get; set; }

	public PersonalVehicleType Type { get; set; }

	public int RequiredLevel { get; set; }

	public MembershipTier MembershipTier { get; set; }

	public VehicleColorSet Colors { get; set; } = new VehicleColorSet();

	public List<int> Liveries { get; set; } = new List<int>();

	public List<int> RoofLiveries { get; set; } = new List<int>();

	public string OverrideMake { get; set; }

	public string OverrideModel { get; set; }

	public string Variant { get; set; }

	public string Notice { get; set; }

	public string Credits { get; set; }

	public bool HasSiren { get; set; }

	public bool IsElectric { get; set; }

	public bool IsUnmarked { get; set; }

	public bool IsJet { get; set; }

	public bool WasRecalled { get; set; }

	public DateTime CreationDate { get; set; }

	public DateTime DisabledDate { get; set; } = DateTime.MaxValue;

	public List<PersonalVehicleModelDiscount> Discounts { get; set; } = new List<PersonalVehicleModelDiscount>();

	public Dictionary<string, object> ExtraData { get; set; } = new Dictionary<string, object>();

	public Dictionary<string, float> ServiceMultipliers { get; set; } = new Dictionary<string, float>();

	public List<string> EconomyMultipliers { get; set; } = new List<string>();

	public bool ShouldAddDefaultMultipliers { get; set; } = true;
}
