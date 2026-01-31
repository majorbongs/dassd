using CitizenFX.Core;

namespace Gtacnr.Model;

public class Impound
{
	public string Name { get; set; }

	public float[] Position_ { get; set; }

	public float[] DropOffPosition_ { get; set; }

	public Vector4 Position => new Vector4(Position_[0], Position_[1], Position_[2], Position_[3]);

	public Vector3 DropOffPosition => new Vector3(DropOffPosition_[0], DropOffPosition_[1], DropOffPosition_[2]);
}
