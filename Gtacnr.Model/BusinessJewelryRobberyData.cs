using System.Collections.Generic;
using CitizenFX.Core;

namespace Gtacnr.Model;

public class BusinessJewelryRobberyData
{
	public float[][] HintCoords_ { get; set; }

	public IEnumerable<Vector3> HintCoords
	{
		get
		{
			//IL_0029: Unknown result type (might be due to invalid IL or missing references)
			if (HintCoords_ == null)
			{
				return null;
			}
			List<Vector3> list = new List<Vector3>();
			float[][] hintCoords_ = HintCoords_;
			foreach (float[] array in hintCoords_)
			{
				list.Add(new Vector3(array[0], array[1], array[2]));
			}
			return list;
		}
	}

	public float[] GasTargetCoords_ { get; set; }

	public Vector4 GasTargetCoords => new Vector4(GasTargetCoords_[0], GasTargetCoords_[1], GasTargetCoords_[2], GasTargetCoords_[3]);

	public float[][] GasCoords_ { get; set; }

	public IEnumerable<Vector3> GasCoords
	{
		get
		{
			//IL_0029: Unknown result type (might be due to invalid IL or missing references)
			if (GasCoords_ == null)
			{
				return null;
			}
			List<Vector3> list = new List<Vector3>();
			float[][] gasCoords_ = GasCoords_;
			foreach (float[] array in gasCoords_)
			{
				list.Add(new Vector3(array[0], array[1], array[2]));
			}
			return list;
		}
	}

	public float[][] GlassCoords_ { get; set; }

	public IEnumerable<Vector4> GlassCoords
	{
		get
		{
			//IL_002c: Unknown result type (might be due to invalid IL or missing references)
			if (GlassCoords_ == null)
			{
				return null;
			}
			List<Vector4> list = new List<Vector4>();
			float[][] glassCoords_ = GlassCoords_;
			foreach (float[] array in glassCoords_)
			{
				list.Add(new Vector4(array[0], array[1], array[2], array[3]));
			}
			return list;
		}
	}

	public string[][] GlassModelSwaps { get; set; }

	public string GateModel { get; set; }

	public float GateSpeed { get; set; }

	public float[][] GateCoords_ { get; set; }

	public IEnumerable<JewelryRobberyGate> Gates
	{
		get
		{
			//IL_002f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0043: Unknown result type (might be due to invalid IL or missing references)
			if (GateCoords_ == null)
			{
				return null;
			}
			List<JewelryRobberyGate> list = new List<JewelryRobberyGate>();
			float[][] gateCoords_ = GateCoords_;
			foreach (float[] array in gateCoords_)
			{
				list.Add(new JewelryRobberyGate
				{
					StartCoords = new Vector3(array[0], array[1], array[2]),
					EndCoords = new Vector3(array[3], array[4], array[5]),
					Heading = array[6]
				});
			}
			return list;
		}
	}

	public int TimeToAlarm { get; set; }

	public int AlarmDuration { get; set; }

	public float[] ExitCoords_ { get; set; }

	public Vector3 ExitCoords => new Vector3(ExitCoords_[0], ExitCoords_[1], ExitCoords_[2]);

	public float LeaveAreaRadius { get; set; }
}
