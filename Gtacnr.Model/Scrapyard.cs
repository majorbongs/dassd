using CitizenFX.Core;

namespace Gtacnr.Model;

public class Scrapyard
{
	public string Name { get; set; }

	public float[] Position_ { get; set; }

	public float Multiplier { get; set; }

	public Vector3 Position => new Vector3(Position_[0], Position_[1], Position_[2]);
}
