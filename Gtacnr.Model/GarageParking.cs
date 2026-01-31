using CitizenFX.Core;
using Gtacnr.Model.Enums;

namespace Gtacnr.Model;

public class GarageParking
{
	public float[] Position_ { get; set; }

	public GarageParkingSize Size { get; set; }

	public Vector3 Position => new Vector3(Position_[0], Position_[1], Position_[2]);

	public float Heading => Position_[3];
}
