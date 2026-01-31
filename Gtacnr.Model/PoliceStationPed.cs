using CitizenFX.Core;
using Gtacnr.Model.Enums;
using Newtonsoft.Json;

namespace Gtacnr.Model;

public class PoliceStationPed
{
	public PoliceStationPedType Type { get; set; }

	public Vector3 Location { get; set; }

	public float Heading { get; set; }

	public float[] Location_
	{
		set
		{
			//IL_000a: Unknown result type (might be due to invalid IL or missing references)
			Location = new Vector3(value[0], value[1], value[2]);
			Heading = value[3];
		}
	}

	[JsonIgnore]
	public Ped Ped { get; set; }

	[JsonIgnore]
	public bool IsRespawning { get; set; }

	[JsonIgnore]
	public PoliceStation PoliceStation { get; set; }
}
