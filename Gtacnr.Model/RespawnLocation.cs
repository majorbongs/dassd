using CitizenFX.Core;
using Newtonsoft.Json;

namespace Gtacnr.Model;

public class RespawnLocation
{
	public string Name { get; set; }

	public float[] Coordinates { get; set; }

	public string Job { get; set; }

	public bool IsCayo { get; set; }

	public float Radio { get; set; }

	[JsonIgnore]
	public Vector3 Position
	{
		get
		{
			//IL_0018: Unknown result type (might be due to invalid IL or missing references)
			return new Vector3(Coordinates[0], Coordinates[1], Coordinates[2]);
		}
		set
		{
			//IL_0009: Unknown result type (might be due to invalid IL or missing references)
			//IL_0012: Unknown result type (might be due to invalid IL or missing references)
			//IL_001b: Unknown result type (might be due to invalid IL or missing references)
			Coordinates = new float[3] { value.X, value.Y, value.Z };
		}
	}

	public float Heading => Coordinates[3];
}
