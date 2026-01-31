namespace Gtacnr.Model;

public class VehicleColorInfo
{
	public int Id { get; set; }

	public string Description { get; set; }

	public string Type { get; set; }

	public VehicleColorInfo(int id, string description)
	{
		Id = id;
		Description = description;
	}

	public override string ToString()
	{
		return Description;
	}
}
