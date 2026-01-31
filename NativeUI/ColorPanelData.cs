using System.Collections.Generic;
using System.Drawing;

namespace NativeUI;

public class ColorPanelData
{
	public Pagination Pagination = new Pagination();

	public int Index;

	public List<Color> Items;

	public string Title;

	public bool Enabled;

	public int Value;
}
