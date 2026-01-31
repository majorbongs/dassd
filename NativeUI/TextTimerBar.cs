using System.Drawing;
using CitizenFX.Core.UI;

namespace NativeUI;

public class TextTimerBar : TimerBarBase
{
	public string Text { get; set; }

	public TextTimerBar(string label, string text)
		: base(label)
	{
		Text = text;
	}

	public override void Draw(int interval)
	{
		SizeF resolutionMaintainRatio = ScreenTools.ResolutionMaintainRatio;
		PointF pointF = ScreenTools.SafezoneBounds;
		base.Draw(interval);
		((Text)new UIResText(Text, new PointF((float)(int)resolutionMaintainRatio.Width - pointF.X - 10f, (float)(int)resolutionMaintainRatio.Height - pointF.Y - (40f + (float)(4 * interval))), 0.45f, base.TextColor, (Font)0, (Alignment)2)).Draw();
	}
}
