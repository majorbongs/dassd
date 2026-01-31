using System;
using System.Drawing;
using System.Threading.Tasks;
using CitizenFX.Core.Native;
using CitizenFX.Core.UI;

namespace NativeUI;

public class UIMenuGridPanel : UIMenuPanel
{
	private UIResText Top;

	private UIResText Left;

	private UIResText Right;

	private UIResText Bottom;

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

	public UIMenuGridPanel(string topText, string bottomText, string leftText, string rightText, PointF circlePosition = default(PointF))
	{
		Enabled = true;
		Background = new Sprite("commonmenu", "gradient_bgd", new Point(0, 0), new Size(431, 275));
		Grid = new Sprite("pause_menu_pages_char_mom_dad", "nose_grid", new Point(0, 0), new Size(180, 180), 0f, Color.FromArgb(185, 185, 185));
		Circle = new Sprite("mpinventory", "in_world_circle", new PointF(0f, 0f), new SizeF(20f, 20f), 0f, Color.FromArgb(225, 225, 225));
		Audio = new UIMenuGridAudio("CONTINUOUS_SLIDER", "HUD_FRONTEND_DEFAULT_SOUNDSET", 0);
		Top = new UIResText(topText ?? "Up", new Point(0, 0), 0.3f, Color.FromArgb(225, 225, 225), (Font)0, (Alignment)0);
		Bottom = new UIResText(bottomText ?? "Down", new Point(0, 0), 0.3f, Color.FromArgb(225, 225, 225), (Font)0, (Alignment)0);
		Left = new UIResText(leftText ?? "Left", new Point(0, 0), 0.3f, Color.FromArgb(225, 225, 225), (Font)0, (Alignment)0);
		Right = new UIResText(rightText ?? "Right", new Point(0, 0), 0.3f, Color.FromArgb(225, 225, 225), (Font)0, (Alignment)0);
		SetCirclePosition = new PointF((circlePosition.X != 0f) ? circlePosition.X : 0.5f, (circlePosition.Y != 0f) ? circlePosition.Y : 0.5f);
	}

	internal override void Position(float y)
	{
		float x = base.ParentItem.Offset.X;
		int widthOffset = base.ParentItem.Parent.WidthOffset;
		Background.Position = new PointF(x, y);
		Grid.Position = new PointF(x + 125.5f + (float)(widthOffset / 2), 47.5f + y);
		((Text)Top).Position = new PointF(x + 215.5f + (float)(widthOffset / 2), 10f + y);
		((Text)Left).Position = new PointF(x + 57.75f + (float)(widthOffset / 2), 120f + y);
		((Text)Right).Position = new PointF(x + 373.25f + (float)(widthOffset / 2), 120f + y);
		((Text)Bottom).Position = new PointF(x + 215.5f + (float)(widthOffset / 2), 235f + y);
		if (!CircleLocked)
		{
			CircleLocked = true;
			CirclePosition = SetCirclePosition;
		}
	}

	internal void UpdateParent(float X, float Y)
	{
		base.ParentItem.Parent.ListChange(base.ParentItem, base.ParentItem.Index);
		base.ParentItem.ListChangedTrigger(base.ParentItem.Index);
	}

	internal async void Functions()
	{
		if (ScreenTools.IsMouseInBounds(new PointF(Grid.Position.X + 20f + safezoneOffset.X, Grid.Position.Y + 20f + safezoneOffset.Y), new SizeF(Grid.Size.Width - 40f, Grid.Size.Height - 40f)))
		{
			if (API.IsDisabledControlPressed(0, 24))
			{
				if (!Pressed)
				{
					Audio.Id = API.GetSoundId();
					API.PlaySoundFrontend(Audio.Id, Audio.Slider, Audio.Library, true);
					Pressed = true;
				}
				float num = API.GetDisabledControlNormal(0, 239) * Resolution.Width;
				float num2 = API.GetDisabledControlNormal(0, 240) * Resolution.Height;
				num -= Circle.Size.Width / 2f + safezoneOffset.X;
				num2 -= Circle.Size.Height / 2f + safezoneOffset.Y;
				PointF position = new PointF((num > Grid.Position.X + 10f + Grid.Size.Width - 40f) ? (Grid.Position.X + 10f + Grid.Size.Width - 40f) : ((num < Grid.Position.X + 20f - Circle.Size.Width / 2f) ? (Grid.Position.X + 20f - Circle.Size.Width / 2f) : num), (num2 > Grid.Position.Y + 10f + Grid.Size.Height - 40f) ? (Grid.Position.Y + 10f + Grid.Size.Height - 40f) : ((num2 < Grid.Position.Y + 20f - Circle.Size.Height / 2f) ? (Grid.Position.Y + 20f - Circle.Size.Height / 2f) : num2));
				Circle.Position = position;
				float num3 = (Circle.Position.X - (Grid.Position.X + 20f) + (Circle.Size.Width + 20f)) / (Grid.Size.Width - 40f) + safezoneOffset.X;
				float num4 = (Circle.Position.Y - (Grid.Position.Y + 20f) + (Circle.Size.Height + 20f)) / (Grid.Size.Height - 40f) + safezoneOffset.Y;
				UpdateParent(((num3 >= 0f && num3 <= 2f) ? num3 : (((num3 <= 0f) ? 0f : 1.2f) * 2f)) - 1f, ((num4 >= 0f && num4 <= 1f) ? num4 : (((num4 <= 0f) ? 0f : 1f) * 2f)) - 1f);
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
			((Text)Top).Draw();
			((Text)Left).Draw();
			((Text)Right).Draw();
			((Text)Bottom).Draw();
			Functions();
			await Task.FromResult(0);
		}
	}
}
