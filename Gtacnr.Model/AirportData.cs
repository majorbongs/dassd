using CitizenFX.Core;

namespace Gtacnr.Model;

public class AirportData
{
	public Business ParentBusiness { get; set; }

	public string Name { get; set; }

	public bool CanBoard { get; set; }

	public float[] ArrivalCoords_ { get; set; }

	public Vector4 ArrivalCoords => new Vector4(ArrivalCoords_[0], ArrivalCoords_[1], ArrivalCoords_[2], ArrivalCoords_[3]);
}
