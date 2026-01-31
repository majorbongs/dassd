namespace Gtacnr.Client.Characters.Editor;

public class EditorEntry
{
	public int Index { get; set; }

	public string Description { get; set; }

	public string DescriptionOrDefault => Description ?? $"#{Index}";

	public override string ToString()
	{
		return DescriptionOrDefault;
	}
}
