using CitizenFX.Core;

namespace Gtacnr.Model;

public class Safezone
{
	public string Description { get; set; }

	public float[] Position { get; set; }

	public float Radius { get; set; }

	public bool Enabled { get; set; } = true;

	public Vector3 Position_ => new Vector3(Position[0], Position[1], Position[2]);

	public Blip Blip { get; set; }
}
