using System.Drawing;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;

namespace MenuAPI;

public class MenuSliderItem : MenuItem
{
	public int Min { get; private set; }

	public int Max { get; private set; } = 10;

	public bool ShowDivider { get; set; }

	public int Position { get; set; }

	public Icon SliderLeftIcon { get; set; }

	public Icon SliderRightIcon { get; set; }

	public Color BackgroundColor { get; set; } = Color.FromArgb(255, 24, 93, 151);

	public Color BarColor { get; set; } = Color.FromArgb(255, 53, 165, 223);

	public MenuSliderItem(string name, int min, int max, int startPosition)
		: this(name, min, max, startPosition, showDivider: false)
	{
	}

	public MenuSliderItem(string name, int min, int max, int startPosition, bool showDivider)
		: this(name, null, min, max, startPosition, showDivider)
	{
	}

	public MenuSliderItem(string name, string description, int min, int max, int startPosition)
		: this(name, description, min, max, startPosition, showDivider: false)
	{
	}

	public MenuSliderItem(string name, string description, int min, int max, int startPosition, bool showDivider)
		: base(name, description)
	{
		Min = min;
		Max = max;
		ShowDivider = showDivider;
		Position = startPosition;
	}

	private float Map(float val, float in_min, float in_max, float out_min, float out_max)
	{
		return (val - in_min) * (out_max - out_min) / (in_max - in_min) + out_min;
	}

	internal override async Task Draw(int indexOffset)
	{
		base.RightIcon = SliderRightIcon;
		base.Label = null;
		await base.Draw(indexOffset);
		if (Position > Max || Position < Min)
		{
			Position = (Max - Min) / 2;
		}
		float num = base.ParentMenu.MenuItemsYOffset + 1f - MenuItem.RowHeight * (float)MathUtil.Clamp(base.ParentMenu.Size, 0, base.ParentMenu.MaxItemsOnScreen);
		float num2 = 150f / MenuController.ScreenWidth;
		float num3 = 10f / MenuController.ScreenHeight;
		float num4 = (base.ParentMenu.Position.Y + (float)(base.Index - indexOffset) * MenuItem.RowHeight + 20f + num) / MenuController.ScreenHeight;
		float num5 = (base.ParentMenu.Position.X + MenuItem.RowWidth) / MenuController.ScreenWidth - num2 / 2f - 8f / MenuController.ScreenWidth;
		if (!base.ParentMenu.LeftAligned)
		{
			num5 = num2 / 2f - 8f / MenuController.ScreenWidth;
		}
		if (SliderLeftIcon != Icon.NONE && SliderRightIcon != Icon.NONE)
		{
			num5 -= 40f / MenuController.ScreenWidth;
			int[] spriteColour = GetSpriteColour(SliderLeftIcon, base.Selected);
			API.SetScriptGfxAlign(base.ParentMenu.LeftAligned ? 76 : 82, 84);
			API.SetScriptGfxAlignParams(0f, 0f, 0f, 0f);
			string spriteDictionary = GetSpriteDictionary(SliderLeftIcon);
			if (base.ParentMenu.LeftAligned)
			{
				API.DrawSprite(spriteDictionary, GetSpriteName(SliderLeftIcon, base.Selected), num5 - (num2 / 2f + 4f / MenuController.ScreenWidth) - GetSpriteSize(SliderLeftIcon, width: true) / 2f, num4, GetSpriteSize(SliderLeftIcon, width: true), GetSpriteSize(SliderLeftIcon, width: false), 0f, spriteColour[0], spriteColour[1], spriteColour[2], 255);
			}
			else
			{
				API.DrawSprite(spriteDictionary, GetSpriteName(SliderLeftIcon, base.Selected), num5 - (num2 + 4f / MenuController.ScreenWidth) - GetSpriteSize(SliderLeftIcon, width: true) - 20f / MenuController.ScreenWidth, num4, GetSpriteSize(SliderLeftIcon, width: true), GetSpriteSize(SliderLeftIcon, width: false), 0f, spriteColour[0], spriteColour[1], spriteColour[2], 255);
			}
			API.ResetScriptGfxAlign();
		}
		API.SetScriptGfxAlign(base.ParentMenu.LeftAligned ? 76 : 82, 84);
		API.SetScriptGfxAlignParams(0f, 0f, 0f, 0f);
		API.DrawRect(num5, num4, num2, num3, (int)BackgroundColor.R, (int)BackgroundColor.G, (int)BackgroundColor.B, (int)BackgroundColor.A);
		float num6 = Map(Position, Min, Max, 0f - num2 / 4f * MenuController.ScreenWidth, num2 / 4f * MenuController.ScreenWidth) / MenuController.ScreenWidth;
		if (!base.ParentMenu.LeftAligned)
		{
			API.DrawRect(num5 - num2 / 2f + num6, num4, num2 / 2f, num3, (int)BarColor.R, (int)BarColor.G, (int)BarColor.B, (int)BarColor.A);
		}
		else
		{
			API.DrawRect(num5 + num6, num4, num2 / 2f, num3, (int)BarColor.R, (int)BarColor.G, (int)BarColor.B, (int)BarColor.A);
		}
		if (ShowDivider)
		{
			if (!base.ParentMenu.LeftAligned)
			{
				API.DrawRect(num5 - num2 + 4f / MenuController.ScreenWidth, num4, 4f / MenuController.ScreenWidth, MenuItem.RowHeight / MenuController.ScreenHeight / 2f, 255, 255, 255, 255);
			}
			else
			{
				API.DrawRect(num5 + 2f / MenuController.ScreenWidth, num4, 4f / MenuController.ScreenWidth, MenuItem.RowHeight / MenuController.ScreenHeight / 2f, 255, 255, 255, 255);
			}
		}
		API.ResetScriptGfxAlign();
	}
}
