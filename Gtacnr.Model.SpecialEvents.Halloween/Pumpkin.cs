using CitizenFX.Core;
using Newtonsoft.Json;

namespace Gtacnr.Model.SpecialEvents.Halloween;

public class Pumpkin
{
	public string Id { get; set; }

	public float[] Position_ { get; set; }

	[JsonIgnore]
	public Vector3 Position => new Vector3(Position_[0], Position_[1], Position_[2]);

	[JsonIgnore]
	public Prop Prop { get; set; }
}
