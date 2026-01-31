using System.Collections.Generic;

namespace Gtacnr.Model;

public class Appearance
{
	public Heritage Heritage { get; set; }

	public List<FaceFeature> FaceFeatures { get; set; }

	public List<ComponentVariation> ComponentVariations { get; set; }

	public List<HeadOverlay> HeadOverlays { get; set; }

	public int HairColor { get; set; }

	public int EyeColor { get; set; }
}
