namespace Gtacnr.Model;

public class ChatMessage
{
	private Color color;

	public ulong Id { get; set; }

	public string Content { get; set; }

	public ChatMessageAuthor Author { get; set; }

	public string OriginalAuthor { get; set; }

	public int Format { get; set; }

	public string ColorString { get; set; }

	public string Prefix { get; set; }

	public string PrefixColorString { get; set; }

	public Color Color
	{
		get
		{
			return color;
		}
		set
		{
			color = value;
			ColorString = color.ToHexString();
		}
	}

	public override string ToString()
	{
		return Content;
	}
}
