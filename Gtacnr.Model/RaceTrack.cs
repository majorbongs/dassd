using System.Collections.Generic;
using CitizenFX.Core;
using Newtonsoft.Json;

namespace Gtacnr.Model;

public class RaceTrack
{
	public List<Vector3> Checkpoints { get; set; } = new List<Vector3>();

	public uint Laps { get; set; } = 1u;

	[JsonIgnore]
	public uint CheckpointsToPass => (uint)Checkpoints.Count * Laps;
}
