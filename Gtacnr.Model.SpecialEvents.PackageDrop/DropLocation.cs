using System.Collections.Generic;
using System.Linq;
using CitizenFX.Core;

namespace Gtacnr.Model.SpecialEvents.PackageDrop;

public class DropLocation
{
	public float[] Position_ { get; set; }

	public float[][] PackageCoords_ { get; set; }

	public Vector3 Position => Position_.ToVector3();

	public IEnumerable<Vector3> PackageCoords => PackageCoords_.Select((float[] l) => l.ToVector3());
}
