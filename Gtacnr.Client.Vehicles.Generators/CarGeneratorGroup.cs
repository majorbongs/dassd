using System;
using System.Collections.Generic;
using System.Linq;
using CitizenFX.Core;

namespace Gtacnr.Client.Vehicles.Generators;

public class CarGeneratorGroup
{
	private Tuple<Vector2, Vector2> cachedBoundaries;

	public string Name { get; set; }

	public float[] Position_ { get; set; }

	public float Range { get; set; } = 200f;

	public List<CarGenerator> Generators { get; set; } = new List<CarGenerator>();

	public Vector3 Position
	{
		get
		{
			//IL_002d: Unknown result type (might be due to invalid IL or missing references)
			//IL_000d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0013: Unknown result type (might be due to invalid IL or missing references)
			if (Position_.Length < 3)
			{
				return default(Vector3);
			}
			return new Vector3(Position_[0], Position_[1], Position_[2]);
		}
	}

	public Tuple<Vector2, Vector2> GetBoundaries()
	{
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ed: Unknown result type (might be due to invalid IL or missing references)
		if (cachedBoundaries != null)
		{
			return cachedBoundaries;
		}
		float x = Generators.Aggregate((CarGenerator g1, CarGenerator g2) => (!(g1.Position.X < g2.Position.X)) ? g2 : g1).Position.X;
		float y = Generators.Aggregate((CarGenerator g1, CarGenerator g2) => (!(g1.Position.Y < g2.Position.Y)) ? g2 : g1).Position.Y;
		float x2 = Generators.Aggregate((CarGenerator g1, CarGenerator g2) => (!(g1.Position.X > g2.Position.X)) ? g2 : g1).Position.X;
		float y2 = Generators.Aggregate((CarGenerator g1, CarGenerator g2) => (!(g1.Position.Y > g2.Position.Y)) ? g2 : g1).Position.Y;
		cachedBoundaries = Tuple.Create<Vector2, Vector2>(new Vector2(x, y), new Vector2(x2, y2));
		return cachedBoundaries;
	}
}
