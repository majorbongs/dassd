using System.Drawing;
using CitizenFX.Core.UI;

namespace NativeUI;

public class BarTimerBar : TimerBarBase
{
	public float Percentage { get; set; }

	public BarColor Color { get; set; }

	public BarTimerBar(string label)
		: base(label)
	{
		Color = BarColors.White;
	}

	public BarTimerBar(string label, float percentage)
		: this(label)
	{
		Percentage = percentage;
	}

	public override void Draw(int interval)
	{
		SizeF resolutionMaintainRatio = ScreenTools.ResolutionMaintainRatio;
		PointF pointF = ScreenTools.SafezoneBounds;
		base.Draw(interval);
		PointF pos = new PointF((float)(int)resolutionMaintainRatio.Width - pointF.X - 160f, (float)(int)resolutionMaintainRatio.Height - pointF.Y - (float)(27 + 4 * interval));
		((Rectangle)new UIResRectangle(pos, new SizeF(150f, 12f), Color.Background)).Draw();
		((Rectangle)new UIResRectangle(pos, new SizeF((int)(150f * Percentage), 12f), Color.Foreground)).Draw();
	}
}
