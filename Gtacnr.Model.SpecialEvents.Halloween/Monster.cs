using CitizenFX.Core;
using Newtonsoft.Json;

namespace Gtacnr.Model.SpecialEvents.Halloween;

public class Monster
{
	[JsonIgnore]
	public Pumpkin ClosestPumpkin;

	public string Model { get; set; }

	public float[] Position_ { get; set; }

	[JsonIgnore]
	public Vector3 Position => new Vector3(Position_[0], Position_[1], Position_[2]);

	[JsonIgnore]
	public float Heading => Position_[3];

	[JsonIgnore]
	public Ped Ped { get; set; }
}
