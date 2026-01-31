using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;

namespace MenuAPI;

public class MenuCheckboxItem : MenuItem
{
	public enum CheckboxStyle
	{
		Cross,
		Tick
	}

	public bool Checked { get; set; }

	public CheckboxStyle Style { get; set; } = CheckboxStyle.Tick;

	public MenuCheckboxItem(string text)
		: this(text, null)
	{
	}

	public MenuCheckboxItem(string text, bool _checked)
		: this(text, null, _checked)
	{
	}

	public MenuCheckboxItem(string text, string description)
		: this(text, description, _checked: false)
	{
	}

	public MenuCheckboxItem(string text, string description, bool _checked)
		: base(text, description)
	{
		Checked = _checked;
	}

	private int GetSpriteColour()
	{
		if (!base.Enabled)
		{
			return 109;
		}
		return 255;
	}

	private string GetSpriteName()
	{
		if (Checked)
		{
			if (Style == CheckboxStyle.Tick)
			{
				if (base.Selected)
				{
					return "shop_box_tickb";
				}
				return "shop_box_tick";
			}
			if (base.Selected)
			{
				return "shop_box_crossb";
			}
			return "shop_box_cross";
		}
		if (base.Selected)
		{
			return "shop_box_blankb";
		}
		return "shop_box_blank";
	}

	private float GetSpriteX()
	{
		bool leftAligned = base.ParentMenu.LeftAligned;
		if (0 == 0)
		{
			if (!leftAligned)
			{
				return API.GetSafeZoneSize() - 20f / MenuController.ScreenWidth;
			}
			return (MenuItem.RowWidth - 20f) / MenuController.ScreenWidth;
		}
		if (!leftAligned)
		{
			return API.GetSafeZoneSize() - (MenuItem.RowWidth - 20f) / MenuController.ScreenWidth;
		}
		return 20f / MenuController.ScreenWidth;
	}

	internal override async Task Draw(int offset)
	{
		base.RightIcon = Icon.NONE;
		base.Label = null;
		await base.Draw(offset);
		API.SetScriptGfxAlign(76, 84);
		API.SetScriptGfxAlignParams(0f, 0f, 0f, 0f);
		float num = base.ParentMenu.MenuItemsYOffset + 1f - MenuItem.RowHeight * (float)MathUtil.Clamp(base.ParentMenu.Size, 0, base.ParentMenu.MaxItemsOnScreen);
		string spriteName = GetSpriteName();
		float num2 = (base.ParentMenu.Position.Y + (float)(base.Index - offset) * MenuItem.RowHeight + 20f + num) / MenuController.ScreenHeight;
		float spriteX = GetSpriteX();
		float num3 = 45f / MenuController.ScreenHeight;
		float num4 = 45f / MenuController.ScreenWidth;
		int spriteColour = GetSpriteColour();
		API.DrawSprite("commonmenu", spriteName, spriteX, num2, num4, num3, 0f, spriteColour, spriteColour, spriteColour, 255);
		API.ResetScriptGfxAlign();
	}
}
