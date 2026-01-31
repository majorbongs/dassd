using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using CitizenFX.Core.UI;

namespace NativeUI.PauseMenu;

public class TabItemSimpleList : TabItem
{
	public Dictionary<string, string> Dictionary { get; set; }

	public TabItemSimpleList(string title, Dictionary<string, string> dict)
		: base(title)
	{
		Dictionary = dict;
		DrawBg = false;
	}

	public override void Draw()
	{
		base.Draw();
		int alpha = ((Focused || !base.CanBeFocused) ? 180 : 60);
		int alpha2 = ((Focused || !base.CanBeFocused) ? 200 : 90);
		int alpha3 = ((Focused || !base.CanBeFocused) ? 255 : 150);
		int num = (int)(base.BottomRight.X - base.TopLeft.X);
		for (int i = 0; i < Dictionary.Count; i++)
		{
			((Rectangle)new UIResRectangle(new PointF(base.TopLeft.X, base.TopLeft.Y + (float)(40 * i)), new SizeF(num, 40f), (i % 2 == 0) ? Color.FromArgb(alpha, 0, 0, 0) : Color.FromArgb(alpha2, 0, 0, 0))).Draw();
			KeyValuePair<string, string> keyValuePair = Dictionary.ElementAt(i);
			((Text)new UIResText(keyValuePair.Key, new PointF(base.TopLeft.X + 6f, base.TopLeft.Y + 5f + (float)(40 * i)), 0.35f, Color.FromArgb(alpha3, Colors.White))).Draw();
			((Text)new UIResText(keyValuePair.Value, new PointF(base.BottomRight.X - 6f, base.TopLeft.Y + 5f + (float)(40 * i)), 0.35f, Color.FromArgb(alpha3, Colors.White), (Font)0, (Alignment)2)).Draw();
		}
	}
}
