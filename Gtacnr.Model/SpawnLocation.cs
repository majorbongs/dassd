using CitizenFX.Core;
using Newtonsoft.Json;

namespace Gtacnr.Model;

public class SpawnLocation
{
	public string Name { get; set; }

	public float[] Position_ { get; set; }

	public string Job { get; set; }

	public float Radio { get; set; }

	public string? RequiredResource { get; set; }

	[JsonIgnore]
	public Vector3 Position => new Vector3(Position_[0], Position_[1], Position_[2]);

	[JsonIgnore]
	public float Heading => Position_[3];

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
