using System;
using System.Drawing;
using System.Threading.Tasks;
using CitizenFX.Core.Native;
using CitizenFX.Core.UI;

namespace NativeUI;

public class UIMenuHorizontalOneLineGridPanel : UIMenuPanel
{
	private UIResText Left;

	private UIResText Right;

	private Sprite Grid;

	private Sprite Circle;

	private UIMenuGridAudio Audio;

	private PointF SetCirclePosition;

	protected bool CircleLocked;

	protected bool Pressed;

	private readonly PointF safezoneOffset = ScreenTools.SafezoneBounds;

	public PointF CirclePosition
	{
		get
		{
			return new PointF((float)Math.Round((Circle.Position.X - (Grid.Position.X + 20f) + Circle.Size.Width / 2f) / (Grid.Size.Width - 40f), 2), (float)Math.Round((Circle.Position.Y - (Grid.Position.Y + 20f) + Circle.Size.Height / 2f) / (Grid.Size.Height - 40f), 2));
		}
		set
		{
			Circle.Position.X = Grid.Position.X + 20f + (Grid.Size.Width - 40f) * ((value.X >= 0f && value.X <= 1f) ? value.X : 0f) - Circle.Size.Width / 2f;
			Circle.Position.Y = Grid.Position.Y + 20f + (Grid.Size.Height - 40f) * ((value.Y >= 0f && value.Y <= 1f) ? value.Y : 0f) - Circle.Size.Height / 2f;
		}
	}

	public UIMenuHorizontalOneLineGridPanel(string LeftText, string RightText, float CirclePositionX = 0.5f)
	{
		Enabled = true;
		Background = new Sprite("commonmenu", "gradient_bgd", new Point(0, 0), new Size(431, 275));
		Grid = new Sprite("NativeUI", "horizontal_grid", new Point(0, 0), new Size(180, 180), 0f, Color.FromArgb(185, 185, 185));
		Circle = new Sprite("mpinventory", "in_world_circle", new Point(0, 0), new Size(20, 20), 0f, Color.FromArgb(225, 225, 225));
		Audio = new UIMenuGridAudio("CONTINUOUS_SLIDER", "HUD_FRONTEND_DEFAULT_SOUNDSET", 0);
		Left = new UIResText(LeftText ?? "Left", new Point(0, 0), 0.3f, Color.FromArgb(225, 225, 225), (Font)0, (Alignment)0);
		Right = new UIResText(RightText ?? "Right", new Point(0, 0), 0.3f, Color.FromArgb(225, 225, 225), (Font)0, (Alignment)0);
		SetCirclePosition = new PointF(CirclePositionX, 0.5f);
	}

	internal override void Position(float y)
	{
		float x = base.ParentItem.Offset.X;
		int widthOffset = base.ParentItem.Parent.WidthOffset;
		Background.Position = new PointF(x, y);
		Grid.Position = new PointF(x + 125.5f + (float)(widthOffset / 2), 47.5f + y);
		((Text)Left).Position = new PointF(x + 55f + (float)(widthOffset / 2), 120f + y);
		((Text)Right).Position = new PointF(x + 375f + (float)(widthOffset / 2), 120f + y);
		if (!CircleLocked)
		{
			CircleLocked = true;
			CirclePosition = SetCirclePosition;
		}
	}

	private void UpdateParent(float X)
	{
		base.ParentItem.Parent.ListChange(base.ParentItem, base.ParentItem.Index);
		base.ParentItem.ListChangedTrigger(base.ParentItem.Index);
	}

	private async void Functions()
	{
		if (ScreenTools.IsMouseInBounds(new PointF(Grid.Position.X + 20f + safezoneOffset.X, Grid.Position.Y + 20f + safezoneOffset.Y), new SizeF(Grid.Size.Width - 40f, Grid.Size.Height - 40f)))
		{
			if (API.IsDisabledControlPressed(0, 24))
			{
				if (!Pressed)
				{
					Pressed = true;
					Audio.Id = API.GetSoundId();
					API.PlaySoundFrontend(Audio.Id, Audio.Slider, Audio.Library, true);
				}
				float num = API.GetDisabledControlNormal(0, 239) * Resolution.Width;
				num -= Circle.Size.Width / 2f + safezoneOffset.X;
				Circle.Position = new PointF((num > Grid.Position.X + 10f + Grid.Size.Width - 40f) ? (Grid.Position.X + 10f + Grid.Size.Width - 40f) : ((num < Grid.Position.X + 20f - Circle.Size.Width / 2f) ? (Grid.Position.X + 20f - Circle.Size.Width / 2f) : num), Circle.Position.Y);
				float num2 = (float)Math.Round((Circle.Position.X - (Grid.Position.X + 20f) + (Circle.Size.Width + 20f)) / (Grid.Size.Width - 40f), 2) + safezoneOffset.X;
				UpdateParent(((num2 >= 0f && num2 <= 1f) ? num2 : (((num2 <= 0f) ? 0f : 1f) * 2f)) - 1f);
			}
			if (API.IsDisabledControlJustReleased(0, 24))
			{
				API.StopSound(Audio.Id);
				API.ReleaseSoundId(Audio.Id);
				Pressed = false;
			}
		}
		else
		{
			API.StopSound(Audio.Id);
			API.ReleaseSoundId(Audio.Id);
			Pressed = false;
		}
	}

	internal override async Task Draw()
	{
		if (Enabled)
		{
			Background.Size = new Size(431 + base.ParentItem.Parent.WidthOffset, 275);
			Background.Draw();
			Grid.Draw();
			Circle.Draw();
			((Text)Left).Draw();
			((Text)Right).Draw();
			Functions();
			await Task.FromResult(0);
		}
	}
}
