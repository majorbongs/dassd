using System.Collections.Generic;
using CitizenFX.Core;
using Newtonsoft.Json;

namespace Gtacnr.Model;

public class TruckingStation
{
	public string Id { get; set; }

	public string Name { get; set; }

	public string PedModel { get; set; }

	public float[] Position_ { get; set; }

	public float[] PedPosition_ { get; set; }

	public List<string> Missions { get; set; }

	public Vector3 Position => new Vector3(Position_[0], Position_[1], Position_[2]);

	public Vector3 PedPosition => new Vector3(PedPosition_[0], PedPosition_[1], PedPosition_[2]);

	public float PedHeading => PedPosition_[3];

	[JsonIgnore]
	public Ped Ped { get; set; }
}
