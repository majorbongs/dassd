using System;
using System.Collections.Generic;
using CitizenFX.Core;

namespace Gtacnr.Model;

public class DrugCorner
{
	public string Id { get; set; }

	public float[] Position_ { get; set; }

	public List<DrugCornerDrugInfo> Drugs { get; set; } = new List<DrugCornerDrugInfo>();

	public float MaxPriceMult { get; set; }

	public float DemandMult { get; set; }

	public float SnitchChance { get; set; }

	public float[][] NPCSpawnPoints_ { get; set; }

	public Vector3 Position => new Vector3(Position_[0], Position_[1], Position_[2]);

	public List<Vector3> NPCSpawnPoints
	{
		get
		{
			//IL_0027: Unknown result type (might be due to invalid IL or missing references)
			List<Vector3> list = new List<Vector3>();
			if (NPCSpawnPoints_ != null)
			{
				float[][] nPCSpawnPoints_ = NPCSpawnPoints_;
				foreach (float[] array in nPCSpawnPoints_)
				{
					list.Add(new Vector3(array[0], array[1], array[2]));
				}
			}
			return list;
		}
	}

	public DateTime LastSnitchTimestamp { get; set; }
}
