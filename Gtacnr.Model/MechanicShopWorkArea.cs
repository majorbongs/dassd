using CitizenFX.Core;
using Newtonsoft.Json;

namespace Gtacnr.Model;

public class MechanicShopWorkArea
{
	public MechanicShopWorkAreaType Type { get; set; }

	[JsonProperty("Location_")]
	public Vector3 Location { get; set; }
}
