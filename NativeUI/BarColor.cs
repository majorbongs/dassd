using System.Drawing;

namespace NativeUI;

public struct BarColor
{
	public Color Foreground { get; set; }

	public Color Background { get; set; }

	public BarColor(Color foreground, Color background)
	{
		Foreground = foreground;
		Background = background;
	}
}
