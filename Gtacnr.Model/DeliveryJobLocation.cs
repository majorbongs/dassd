using System.Collections.Generic;
using CitizenFX.Core;
using Gtacnr.Model.Enums;
using Newtonsoft.Json;

namespace Gtacnr.Model;

public class DeliveryJobLocation
{
	public string Name { get; set; }

	[JsonProperty("Coordinates")]
	private float[] _coordinates { get; set; }

	public IEnumerable<DeliveryJobType> AllowedJobTypes { get; set; }

	public DeliveryJobLocationType Type { get; set; }

	public string? RequiredResource { get; set; }

	[JsonIgnore]
	public bool IsPickup => Type != DeliveryJobLocationType.Dropoff;

	[JsonIgnore]
	public bool IsDropoff => Type != DeliveryJobLocationType.Pickup;

	[JsonIgnore]
	public Vector4 Coordinates => _coordinates.ToVector4();

	public bool ExistsOnMap()
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
