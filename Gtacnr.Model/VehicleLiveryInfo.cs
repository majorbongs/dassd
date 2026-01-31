namespace Gtacnr.Model;

public class VehicleLiveryInfo
{
	public int Index { get; set; }

	public VehicleLiveryInfo(int index)
	{
		Index = index;
	}

	public override string ToString()
	{
		return $"{Index}";
	}
}
