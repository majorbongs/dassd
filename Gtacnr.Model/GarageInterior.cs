using System.Collections.Generic;
using CitizenFX.Core;
using Gtacnr.Model.Enums;

namespace Gtacnr.Model;

public class GarageInterior
{
	public string Id { get; set; }

	public GarageSize Size { get; set; }

	public float[] OnFootPosition_ { get; set; }

	public float[] ManagePosition_ { get; set; }

	public List<GarageParking> ParkingSpaces { get; set; }

	public Vector3 OnFootPosition => new Vector3(OnFootPosition_[0], OnFootPosition_[1], OnFootPosition_[2]);

	public Vector3 ManagePosition => new Vector3(ManagePosition_[0], ManagePosition_[1], ManagePosition_[2]);

	public float OnFootHeading => OnFootPosition_[3];
}
