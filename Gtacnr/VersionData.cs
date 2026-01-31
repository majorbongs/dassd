namespace Gtacnr;

public class VersionData
{
	public static VersionData Current = new VersionData
	{
		Name = "Cops and Robbers V",
		Version = "v0.3.365",
		Changes = "Meta Changes"
	};

	public string? Name { get; set; }

	public string? Version { get; set; }

	public string? Changes { get; set; }
}
