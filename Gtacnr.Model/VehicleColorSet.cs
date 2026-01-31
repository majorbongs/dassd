using System.Collections.Generic;

namespace Gtacnr.Model;

public class VehicleColorSet
{
	public List<int> Primary { get; set; } = new List<int>();

	public List<int> Secondary { get; set; } = new List<int>();

	public List<int> Trim { get; set; } = new List<int>();

	public List<int> Dashboard { get; set; } = new List<int>();
}
