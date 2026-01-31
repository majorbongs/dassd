namespace Gtacnr.Model;

public class ChatMessageAuthor
{
	private Color color;

	public string Name { get; set; }

	public int Id { get; set; }

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

	public string ColorString { get; set; }

	public override string ToString()
	{
		return Name;
	}
}
