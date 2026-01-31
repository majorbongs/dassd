using System.Collections.Generic;
using CitizenFX.Core;

namespace Gtacnr.Model;

public class BusinessSafe
{
	public float[] Position_ { get; set; }

	public Vector4 Position
	{
		get
		{
			//IL_0030: Unknown result type (might be due to invalid IL or missing references)
			//IL_0036: Unknown result type (might be due to invalid IL or missing references)
			//IL_0028: Unknown result type (might be due to invalid IL or missing references)
			if (Position_ != null)
			{
				return new Vector4(Position_[0], Position_[1], Position_[2], Position_[3]);
			}
			return default(Vector4);
		}
	}

	public float[][] Positions_ { get; set; }

	public IEnumerable<Vector4> Positions
	{
		get
		{
			//IL_002c: Unknown result type (might be due to invalid IL or missing references)
			if (Positions_ == null)
			{
				return null;
			}
			List<Vector4> list = new List<Vector4>();
			float[][] positions_ = Positions_;
			foreach (float[] array in positions_)
			{
				list.Add(new Vector4(array[0], array[1], array[2], array[3]));
			}
			return list;
		}
	}

	public float SuccessRate { get; set; } = 0.7f;

	public float AmountMultiplier { get; set; } = 1f;
}
