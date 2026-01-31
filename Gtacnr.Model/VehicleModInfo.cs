namespace Gtacnr.Model;

public class VehicleModInfo
{
	public int Type { get; set; }

	public int Index { get; set; }

	public VehicleModInfo(int type, int index)
	{
		Type = type;
		Index = index;
	}

	public override string ToString()
	{
		return $"{Index}";
	}
}
