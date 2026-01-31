using CitizenFX.Core;
using Gtacnr.Model;
using Gtacnr.Model.Enums;
using Newtonsoft.Json;

namespace Gtacnr.Client.Businesses.Hospitals;

public class HospitalPed
{
	public Ped Ped { get; set; }

	public Hospital Hospital { get; set; }

	public Sex Sex { get; set; }

	[JsonIgnore]
	public bool IsRespawning { get; set; }
}
