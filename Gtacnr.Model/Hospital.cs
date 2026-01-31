namespace Gtacnr.Model;

public class Hospital
{
	public Business ParentBusiness { get; set; }

	public string Department { get; set; }

	public bool IsCayo { get; set; }

	public bool IsPillbox { get; set; }

	public string Dealership { get; set; }
}
