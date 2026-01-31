using System;
using System.Collections.Generic;
using System.Linq;
using CitizenFX.Core;
using CitizenFX.Core.Native;

namespace Gtacnr.Client.Vehicles.Generators;

public class CarGenerator
{
	public DateTime LastSpawnTimestamp = DateTime.MinValue;

	private List<uint> modelHashes;

	private List<List<int>> modelLiveries;

	public float[] Position_ { get; set; }

	public List<string> Models { get; set; }

	public Vector4 Position
	{
		get
		{
			//IL_000d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0013: Unknown result type (might be due to invalid IL or missing references)
			//IL_0063: Unknown result type (might be due to invalid IL or missing references)
			//IL_003d: Unknown result type (might be due to invalid IL or missing references)
			if (Position_.Length < 3)
			{
				return default(Vector4);
			}
			if (Position_.Length < 4)
			{
				return new Vector4(Position_[0], Position_[1], Position_[2], 0f);
			}
			return new Vector4(Position_[0], Position_[1], Position_[2], Position_[3]);
		}
	}

	public List<uint> ModelHashes
	{
		get
		{
			if (modelHashes == null)
			{
				modelHashes = Models.Select((string m) => (uint)API.GetHashKey((!m.Contains("@")) ? m : m.Substring(0, m.IndexOf("@")))).ToList();
			}
			return modelHashes;
		}
	}

	public List<List<int>> ModelLiveries
	{
		get
		{
			if (modelLiveries == null)
			{
				modelLiveries = new List<List<int>>();
				foreach (string model in Models)
				{
					List<int> list = new List<int>();
					if (model.Contains("@"))
					{
						string text = model.Substring(model.IndexOf("@") + 1, model.Length - model.IndexOf("@") - 1);
						if (text.Length > 0)
						{
							int result4;
							if (text.Contains("-"))
							{
								string[] array = text.Split('-');
								if (int.TryParse(array[0], out var result) && int.TryParse(array[1], out var result2) && result < result2)
								{
									for (int i = result; i <= result2; i++)
									{
										list.Add(i);
									}
								}
							}
							else if (text.Contains(","))
							{
								string[] array2 = text.Split(',');
								for (int j = 0; j < array2.Length; j++)
								{
									if (int.TryParse(array2[j], out var result3))
									{
										list.Add(result3);
									}
								}
							}
							else if (int.TryParse(text, out result4))
							{
								list.Add(result4);
							}
						}
					}
					modelLiveries.Add(list);
				}
			}
			return modelLiveries;
		}
	}
}
