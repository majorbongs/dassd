using System.Collections.Generic;
using CitizenFX.Core;

namespace Gtacnr.Model;

public class GasStation
{
	public string Name { get; set; }

	public GasStationType Type { get; set; }

	public float PriceMultiplier { get; set; }

	public string BusinessId { get; set; }

	public float[] Position_ { get; set; }

	public float[][] Pumps_ { get; set; }

	public float[][] Chargers_ { get; set; }

	public bool CreateBlip { get; set; }

	public List<string> AllowedJobs { get; set; }

	public Vector3 Position => new Vector3(Position_[0], Position_[1], Position_[2]);

	public List<Vector4> Pumps
	{
		get
		{
			//IL_0022: Unknown result type (might be due to invalid IL or missing references)
			List<Vector4> list = new List<Vector4>();
			float[][] pumps_ = Pumps_;
			foreach (float[] array in pumps_)
			{
				list.Add(new Vector4(array[0], array[1], array[2], array[3]));
			}
			return list;
		}
	}

	public List<Vector4> Chargers
	{
		get
		{
			//IL_002a: Unknown result type (might be due to invalid IL or missing references)
			List<Vector4> list = new List<Vector4>();
			if (Chargers_ != null)
			{
				float[][] chargers_ = Chargers_;
				foreach (float[] array in chargers_)
				{
					list.Add(new Vector4(array[0], array[1], array[2], array[3]));
				}
			}
			return list;
		}
	}
}
