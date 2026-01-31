using System;
using System.Collections.Generic;
using CitizenFX.Core;
using CitizenFX.Core.Native;

namespace Gtacnr.Model;

public class AppliedApparelData
{
	public Dictionary<int, Tuple<int, int, int>> ComponentVariations { get; set; } = new Dictionary<int, Tuple<int, int, int>>();

	public Dictionary<int, Tuple<int, int>> PropVariations { get; set; } = new Dictionary<int, Tuple<int, int>>();

	public bool DoesMatch(Ped ped, out string? error)
	{
		error = null;
		int handle = ((PoolObject)ped).Handle;
		foreach (int key in ComponentVariations.Keys)
		{
			Tuple<int, int, int> tuple = ComponentVariations[key];
			int item = tuple.Item1;
			int item2 = tuple.Item2;
			int item3 = tuple.Item3;
			if (API.GetPedDrawableVariation(handle, key) != item)
			{
				error = $"Invalid drawable variation of {key}: {API.GetPedDrawableVariation(handle, key)} != {item}";
				return false;
			}
			if (item != 0)
			{
				if (API.GetPedTextureVariation(handle, key) != item2)
				{
					error = $"Invalid component texture variation of {key}: {API.GetPedTextureVariation(handle, key)} != {item2}";
					return false;
				}
				if (API.GetPedPaletteVariation(handle, key) != item3)
				{
					error = $"Invalid component palette variation of {key}: {API.GetPedPaletteVariation(handle, key)} != {item3}";
					return false;
				}
			}
		}
		foreach (int key2 in PropVariations.Keys)
		{
			Tuple<int, int> tuple2 = PropVariations[key2];
			int item4 = tuple2.Item1;
			int item5 = tuple2.Item2;
			int pedPropIndex = API.GetPedPropIndex(handle, key2);
			if (pedPropIndex != -1)
			{
				if (pedPropIndex != item4)
				{
					error = $"Invalid prop variation of {key2}: {pedPropIndex} != {item4}";
					return false;
				}
				if (item4 != -1 && API.GetPedPropTextureIndex(handle, key2) != item5)
				{
					error = $"Invalid prop texture variation of {key2}: {API.GetPedPropTextureIndex(handle, key2)} != {item5}";
					return false;
				}
			}
		}
		return true;
	}
}
