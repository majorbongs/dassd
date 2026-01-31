using System;
using System.Drawing;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using CitizenFX.Core.UI;

namespace NativeUI;

public class UIMenuVerticalOneLineGridPanel : UIMenuPanel
{
	private UIResText Top;

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

	public UIMenuVerticalOneLineGridPanel(string TopText, string BottomText, float circlePositionY = 0.5f)
	{
		Enabled = true;
		Background = new Sprite("commonmenu", "gradient_bgd", new Point(0, 0), new Size(431, 275));
		Grid = new Sprite("NativeUI", "vertical_grid", new Point(0, 0), new Size(200, 200), 0f, Color.FromArgb(255, 255, 255));
		Circle = new Sprite("mpinventory", "in_world_circle", new Point(0, 0), new Size(20, 20), 0f, Color.FromArgb(255, 255, 255));
		Audio = new UIMenuGridAudio("CONTINUOUS_SLIDER", "HUD_FRONTEND_DEFAULT_SOUNDSET", 0);
		Top = new UIResText(TopText ?? "Up", new Point(0, 0), 0.35f, Color.FromArgb(255, 255, 255), (Font)0, (Alignment)0);
		Bottom = new UIResText(BottomText ?? "Down", new Point(0, 0), 0.35f, Color.FromArgb(255, 255, 255), (Font)0, (Alignment)0);
		SetCirclePosition = new PointF(0.5f, (circlePositionY != 0f) ? circlePositionY : 0.5f);
	}

	internal override void Position(float y)
	{
		float x = base.ParentItem.Offset.X;
		int widthOffset = base.ParentItem.Parent.WidthOffset;
		Background.Position = new PointF(x, y);
		Grid.Position = new PointF(x + 115.5f + (float)(widthOffset / 2), 37.5f + y);
		((Text)Top).Position = new PointF(x + 215.5f + (float)(widthOffset / 2), 5f + y);
		((Text)Bottom).Position = new PointF(x + 215.5f + (float)(widthOffset / 2), 240f + y);
		if (!CircleLocked)
		{
			CircleLocked = true;
			CirclePosition = SetCirclePosition;
		}
	}

	private void UpdateParent(float Y)
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
				await BaseScript.Delay(0);
				float num = API.GetDisabledControlNormal(0, 240) * Resolution.Height;
				num -= Circle.Size.Height / 2f + safezoneOffset.Y;
				Circle.Position = new PointF(Circle.Position.X, (num > Grid.Position.Y + 10f + Grid.Size.Height - 40f) ? (Grid.Position.Y + 10f + Grid.Size.Height - 40f) : ((num < Grid.Position.Y + 20f - Circle.Size.Height / 2f) ? (Grid.Position.Y + 20f - Circle.Size.Height / 2f) : num));
				float num2 = (float)Math.Round((Circle.Position.Y - (Grid.Position.Y + 20f) + (Circle.Size.Height + 20f)) / (Grid.Size.Height - 40f), 2);
				UpdateParent(((num2 >= 0f && num2 <= 1f) ? num2 : (((num2 <= 0f) ? 0f : 1f) * 2f)) - 1f);
			}
			else
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
			((Text)Bottom).Draw();
			Functions();
			await Task.FromResult(0);
		}
	}
}
