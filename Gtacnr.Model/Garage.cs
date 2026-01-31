using CitizenFX.Core;
using Gtacnr.Model.Enums;
using Newtonsoft.Json;

namespace Gtacnr.Model;

public class Garage
{
	public string Id { get; set; }

	public string Name { get; set; }

	public string InteriorId { get; set; }

	public int Value { get; set; }

	public int DailyCosts { get; set; }

	public int RequiredLevel { get; set; }

	public MembershipTier MembershipTier { get; set; }

	public int Coins { get; set; }

	public float[] OnFootPosition_ { get; set; }

	public float[] VehiclePosition_ { get; set; }

	public Vector3 OnFootPosition => new Vector3(OnFootPosition_[0], OnFootPosition_[1], OnFootPosition_[2]);

	public Vector3 VehiclePosition => new Vector3(VehiclePosition_[0], VehiclePosition_[1], VehiclePosition_[2]);

	public float OnFootHeading => OnFootPosition_[3];

	public float VehicleHeading => VehiclePosition_[3];

	[JsonIgnore]
	public GarageInterior Interior { get; set; }
}
