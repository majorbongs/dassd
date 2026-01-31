using System;
using System.Drawing;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using CitizenFX.Core.UI;

namespace NativeUI;

public class UIMenuPercentagePanel : UIMenuPanel
{
	private UIResRectangle ActiveBar;

	private UIResRectangle BackgroundBar;

	private UIMenuGridAudio Audio;

	private UIResText Min;

	private UIResText Max;

	private UIResText Title;

	private readonly PointF safezoneOffset = ScreenTools.SafezoneBounds;

	private bool Pressed;

	public float Percentage
	{
		get
		{
			float num = (float)Math.Round(API.GetDisabledControlNormal(0, 239) * Resolution.Width) - ((Rectangle)ActiveBar).Position.X;
			return (float)Math.Round(((num >= 0f && num <= 413f) ? num : ((float)((!(num < 0f)) ? 413 : 0))) / Background.Size.Width, 2);
		}
		set
		{
			float num = ((value < 0f) ? 0f : ((value > 1f) ? 1f : value));
			((Rectangle)ActiveBar).Size = new SizeF(((Rectangle)BackgroundBar).Size.Width * num, ((Rectangle)ActiveBar).Size.Height);
		}
	}

	public UIMenuPercentagePanel(string title, string MinText, string MaxText)
	{
		Enabled = true;
		Background = new Sprite("commonmenu", "gradient_bgd", new Point(0, 0), new Size(431, 275));
		ActiveBar = new UIResRectangle(new Point(0, 0), new Size(413, 10), Color.FromArgb(245, 245, 245));
		BackgroundBar = new UIResRectangle(new Point(0, 0), new Size(413, 10), Color.FromArgb(80, 80, 80));
		Min = new UIResText((MinText != "" || MinText != null) ? MinText : "0%", new Point(0, 0), 0.35f, Color.FromArgb(255, 255, 255), (Font)0, (Alignment)0);
		Max = new UIResText((MaxText != "" || MaxText != null) ? MaxText : "100%", new Point(0, 0), 0.35f, Color.FromArgb(255, 255, 255), (Font)0, (Alignment)0);
		Title = new UIResText((title != "" || title != null) ? title : "Opacity", new Point(0, 0), 0.35f, Color.FromArgb(255, 255, 255), (Font)0, (Alignment)0);
		Audio = new UIMenuGridAudio("CONTINUOUS_SLIDER", "HUD_FRONTEND_DEFAULT_SOUNDSET", 0);
	}

	internal override void Position(float y)
	{
		float x = base.ParentItem.Offset.X;
		int widthOffset = base.ParentItem.Parent.WidthOffset;
		Background.Position = new PointF(x, y);
		((Rectangle)ActiveBar).Position = new PointF(x + (float)(widthOffset / 2) + 9f, 50f + y);
		((Rectangle)BackgroundBar).Position = ((Rectangle)ActiveBar).Position;
		((Text)Min).Position = new PointF(x + (float)(widthOffset / 2) + 25f, 15f + y);
		((Text)Max).Position = new PointF(x + (float)(widthOffset / 2) + 398f, 15f + y);
		((Text)Title).Position = new PointF(x + (float)(widthOffset / 2) + 215.5f, 15f + y);
	}

	public void UpdateParent(float Percentage)
	{
		base.ParentItem.Parent.ListChange(base.ParentItem, base.ParentItem.Index);
		base.ParentItem.ListChangedTrigger(base.ParentItem.Index);
	}

	private async void Functions()
	{
		if (ScreenTools.IsMouseInBounds(new PointF(((Rectangle)BackgroundBar).Position.X + safezoneOffset.X, ((Rectangle)BackgroundBar).Position.Y - 4f + safezoneOffset.Y), new SizeF(((Rectangle)BackgroundBar).Size.Width, ((Rectangle)BackgroundBar).Size.Height + 8f)))
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
				float num = API.GetDisabledControlNormal(0, 239) * Resolution.Width;
				num -= ((Rectangle)ActiveBar).Position.X + safezoneOffset.X;
				((Rectangle)ActiveBar).Size = new SizeF((num >= 0f && num <= 413f) ? num : ((float)((!(num < 0f)) ? 413 : 0)), ((Rectangle)ActiveBar).Size.Height);
				UpdateParent((float)Math.Round((num >= 0f && num <= 413f) ? num : ((float)((!(num < 0f)) ? 413 : 0) / ((Rectangle)BackgroundBar).Size.Width), 2));
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
			Background.Size = new Size(431 + base.ParentItem.Parent.WidthOffset, 76);
			Background.Draw();
			((Rectangle)BackgroundBar).Draw();
			((Rectangle)ActiveBar).Draw();
			((Text)Min).Draw();
			((Text)Max).Draw();
			((Text)Title).Draw();
			Functions();
		}
		await Task.FromResult(0);
	}
}
