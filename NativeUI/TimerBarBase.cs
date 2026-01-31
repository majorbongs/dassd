using System.Drawing;
using CitizenFX.Core.UI;

namespace NativeUI;

public abstract class TimerBarBase
{
	public string Label { get; set; }

	public Color TextColor { get; set; } = Colors.White;

	public TimerBarBase(string label)
	{
		Label = label;
	}

	public virtual void Draw(int interval)
	{
		SizeF resolutionMaintainRatio = ScreenTools.ResolutionMaintainRatio;
		PointF pointF = ScreenTools.SafezoneBounds;
		((Text)new UIResText(Label, new PointF((float)(int)resolutionMaintainRatio.Width - pointF.X - 180f, (float)(int)resolutionMaintainRatio.Height - pointF.Y - (29.75f + (float)(4 * interval))), 0.3f, TextColor, (Font)0, (Alignment)2)).Draw();
		new Sprite("timerbars", "all_black_bg", new PointF((float)(int)resolutionMaintainRatio.Width - pointF.X - 298f, (float)(int)resolutionMaintainRatio.Height - pointF.Y - (float)(40 + 4 * interval)), new SizeF(300f, 37f), 0f, Color.FromArgb(180, 255, 255, 255)).Draw();
	}
}
