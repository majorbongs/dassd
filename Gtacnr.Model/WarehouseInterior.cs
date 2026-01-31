using System.Collections.Generic;
using CitizenFX.Core;

namespace Gtacnr.Model;

public class WarehouseInterior
{
	public string Id { get; set; }

	public float Capacity { get; set; }

	public float[] OnFootPosition_ { get; set; }

	public float[] VehiclePosition_ { get; set; }

	public float[] ManagePosition_ { get; set; }

	public List<float[]> DropPositions_ { get; set; }

	public Vector3 OnFootPosition => new Vector3(OnFootPosition_[0], OnFootPosition_[1], OnFootPosition_[2]);

	public Vector3 VehiclePosition => new Vector3(VehiclePosition_[0], VehiclePosition_[1], VehiclePosition_[2]);

	public Vector3 ManagePosition => new Vector3(ManagePosition_[0], ManagePosition_[1], ManagePosition_[2]);

	public float OnFootHeading => OnFootPosition_[3];

	public float VehicleHeading => VehiclePosition_[3];

	public float ManageHeading => ManagePosition_[3];
}
