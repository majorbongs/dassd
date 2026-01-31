using System.Collections.Generic;
using CitizenFX.Core;

namespace Gtacnr.Model;

public class AircraftStand
{
	public string Id { get; set; }

	public string Name { get; set; }

	public float[] Position_ { get; set; }

	public List<string> Services { get; set; } = new List<string>();

	public float PriceMultiplier { get; set; }

	public Vector3 Position => new Vector3(Position_[0], Position_[1], Position_[2]);
}
