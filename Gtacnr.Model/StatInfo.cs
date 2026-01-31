namespace Gtacnr.Model;

public class StatInfo
{
	public string? Id { get; set; }

	public string? Name { get; set; }

	public string? Description { get; set; }

	public StatCategory Category { get; set; }

	public StatType Type { get; set; }

	public StatExtraType ExtraType { get; set; }

	public string? ExtraName { get; set; }

	public string? ExtraDescription { get; set; }

	public bool Hide { get; set; }

	public override string ToString()
	{
		return Name ?? "";
	}
}
