namespace Gtacnr.Model;

public class VehicleTintInfo
{
	public int Index { get; set; }

	public VehicleTintInfo(int index)
	{
		Index = index;
	}

	public override string ToString()
	{
		return $"{Index}";
	}
}
