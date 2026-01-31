using CitizenFX.Core;

namespace Gtacnr.Model;

public class JobGarage
{
	public string Id { get; set; }

	public string Name { get; set; }

	public string Job { get; set; }

	public float[] OnFootPosition_ { get; set; }

	public float[] VehiclePosition_ { get; set; }

	public Vector3 OnFootPosition => new Vector3(OnFootPosition_[0], OnFootPosition_[1], OnFootPosition_[2]);

	public Vector3 VehiclePosition => new Vector3(VehiclePosition_[0], VehiclePosition_[1], VehiclePosition_[2]);

	public float OnFootHeading => OnFootPosition_[3];

	public float VehicleHeading => VehiclePosition_[3];

	public string? RequiredResource { get; set; }

	public bool HasRequiredResource()
	{
		if (string.IsNullOrWhiteSpace(RequiredResource))
		{
			return true;
		}
		if (RequiredResource.StartsWith("!"))
		{
			return !Utils.IsResourceLoadedOrLoading(RequiredResource.Substring(1));
		}
		return Utils.IsResourceLoadedOrLoading(RequiredResource);
	}
}
