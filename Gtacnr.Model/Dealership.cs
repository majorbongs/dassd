using System.Collections.Generic;
using CitizenFX.Core;
using Gtacnr.Model.Enums;
using Newtonsoft.Json;

namespace Gtacnr.Model;

public class Dealership
{
	public List<DealershipSupply> Supplies = new List<DealershipSupply>();

	public Business ParentBusiness { get; set; }

	public DealershipType Type { get; set; }

	public Dictionary<string, float[]> Cameras { get; set; } = new Dictionary<string, float[]>();

	public List<float[]> ParkedCars { get; set; }

	public List<string> AllowedJobs { get; set; }

	public bool IsRental { get; set; }

	[JsonProperty("CarPosition_")]
	public Vector4 CarPosition { get; set; }

	[JsonProperty("CarOutPosition_")]
	public Vector4 CarOutPosition { get; set; }

	[JsonProperty("PlayerLookPosition_")]
	public Vector4 PlayerLookPosition { get; set; }

	[JsonProperty("DealerLookPosition_")]
	public Vector4 DealerLookPosition { get; set; }
}
