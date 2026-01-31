using System.Collections.Generic;
using System.Runtime.Serialization;
using CitizenFX.Core;
using Newtonsoft.Json;

namespace Gtacnr.Model;

public class PoliceStation
{
	public Dictionary<string, Vector4> ReleaseLocationResourceOverrides = new Dictionary<string, Vector4>();

	public Business ParentBusiness { get; set; }

	public string Department { get; set; }

	public string Dealership { get; set; }

	[JsonProperty("ReleaseLocation_")]
	public Vector4 ReleaseLocation { get; set; }

	[OnDeserialized]
	internal void OnDeserializedMethod(StreamingContext context)
	{
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		foreach (string key in ReleaseLocationResourceOverrides.Keys)
		{
			Vector4 releaseLocation = ReleaseLocationResourceOverrides[key];
			if (Utils.IsResourceLoadedOrLoading(key))
			{
				ReleaseLocation = releaseLocation;
			}
		}
	}
}
