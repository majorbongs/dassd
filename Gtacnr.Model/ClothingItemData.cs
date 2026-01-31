using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Gtacnr.Model;

public class ClothingItemData
{
	public IEnumerable<ComponentVariation> Components
	{
		get
		{
			List<ComponentVariation> list = new List<ComponentVariation>();
			foreach (int[] item in Components_)
			{
				list.Add(new ComponentVariation
				{
					Index = item[0],
					Drawable = item[1],
					Texture = item[2]
				});
			}
			return list;
		}
	}

	public IEnumerable<Accessory> Props
	{
		get
		{
			List<Accessory> list = new List<Accessory>();
			foreach (int[] item in Props_)
			{
				list.Add(new Accessory
				{
					Index = item[0],
					Drawable = item[1],
					Texture = item[2]
				});
			}
			return list;
		}
	}

	public IEnumerable<HeadOverlay> HeadOverlays
	{
		get
		{
			List<HeadOverlay> list = new List<HeadOverlay>();
			foreach (float[] item in HeadOverlays_)
			{
				list.Add(new HeadOverlay
				{
					Index = Convert.ToInt32(item[0]),
					Overlay = Convert.ToInt32(item[1]),
					Opacity = item[2],
					Color = Convert.ToInt32(item[3])
				});
			}
			return list;
		}
	}

	public IEnumerable<Decoration> Decorations
	{
		get
		{
			List<Decoration> list = new List<Decoration>();
			foreach (string[] item in Decorations_)
			{
				list.Add(new Decoration
				{
					Collection = item[0],
					Name = item[1]
				});
			}
			return list;
		}
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	public List<int[]> Components_ { get; set; } = new List<int[]>();

	[EditorBrowsable(EditorBrowsableState.Never)]
	public List<int[]> Props_ { get; set; } = new List<int[]>();

	[EditorBrowsable(EditorBrowsableState.Never)]
	public List<float[]> HeadOverlays_ { get; set; } = new List<float[]>();

	[EditorBrowsable(EditorBrowsableState.Never)]
	public List<string[]> Decorations_ { get; set; } = new List<string[]>();
}
