using CitizenFX.Core;
using Newtonsoft.Json;

namespace Gtacnr.Model;

public class Redzone
{
	public string Description { get; private set; }

	[JsonIgnore]
	public Vector3 Position { get; private set; }

	public float Radius { get; private set; }

	public float Chance { get; set; } = 1f;

	[JsonProperty("Position")]
	private float[] PositionArray => new float[3] { Position.X, Position.Y, Position.Z };

	[JsonConstructor]
	public Redzone(string description, float[] position, float radius)
	{
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		Description = description;
		Position = new Vector3(position[0], position[1], position[2]);
		Radius = radius;
	}

	public bool IsPointInside(Vector3 point)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		Vector3 position = Position;
		return ((Vector3)(ref position)).DistanceToSquared2D(point) < Radius.Square();
	}
}
