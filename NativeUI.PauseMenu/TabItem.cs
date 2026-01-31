using System;
using System.Drawing;
using CitizenFX.Core.UI;

namespace NativeUI.PauseMenu;

public class TabItem
{
	protected SizeF Resolution = ScreenTools.ResolutionMaintainRatio;

	public bool DrawBg;

	protected Sprite RockstarTile;

	public virtual bool Visible { get; set; }

	public virtual bool Focused { get; set; }

	public string Title { get; set; }

	public bool Active { get; set; }

	public bool JustOpened { get; set; }

	public bool CanBeFocused { get; set; }

	public PointF TopLeft { get; set; }

	public PointF BottomRight { get; set; }

	public PointF SafeSize { get; set; }

	public bool UseDynamicPositionment { get; set; }

	public TabView Parent { get; set; }

	public bool FadeInWhenFocused { get; set; }

	public event EventHandler Activated;

	public event EventHandler DrawInstructionalButtons;

	public TabItem(string name)
	{
		RockstarTile = new Sprite("pause_menu_sp_content", "rockstartilebmp", default(PointF), new SizeF(64f, 64f), 0f, Color.FromArgb(40, 255, 255, 255));
		Title = name;
		DrawBg = true;
		UseDynamicPositionment = true;
	}

	public void OnActivated()
	{
		this.Activated?.Invoke(this, EventArgs.Empty);
	}

	public virtual void ProcessControls()
	{
	}

	public virtual void Draw()
	{
		if (!Visible)
		{
			return;
		}
		if (UseDynamicPositionment)
		{
			SafeSize = new PointF(300f, (Parent.SubTitle != null && Parent.SubTitle != "") ? 255 : 245);
			TopLeft = new PointF(SafeSize.X, SafeSize.Y);
			BottomRight = new PointF((float)(int)Resolution.Width - SafeSize.X, (float)(int)Resolution.Height - SafeSize.Y);
		}
		SizeF size = new SizeF(BottomRight.SubtractPoints(TopLeft));
		this.DrawInstructionalButtons?.Invoke(this, EventArgs.Empty);
		if (DrawBg)
		{
			((Rectangle)new UIResRectangle(TopLeft, size, Color.FromArgb((Focused || !FadeInWhenFocused) ? 200 : 120, 0, 0, 0))).Draw();
			int num = 100;
			RockstarTile.Size = new SizeF(num, num);
			float num2 = size.Width / (float)num;
			int num3 = 4;
			for (int i = 0; (float)i < num2 * (float)num3; i++)
			{
				RockstarTile.Position = TopLeft.AddPoints(new PointF(num * (i % (int)num2), num * i / (int)num2));
				RockstarTile.Color = Color.FromArgb((int)MiscExtensions.LinearFloatLerp(20f, 0f, i / (int)num2, num3), 255, 255, 255);
				RockstarTile.Draw();
			}
		}
	}
}
