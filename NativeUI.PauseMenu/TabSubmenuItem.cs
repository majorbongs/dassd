using System.Collections.Generic;
using System.Drawing;
using CitizenFX.Core;
using CitizenFX.Core.UI;

namespace NativeUI.PauseMenu;

public class TabSubmenuItem : TabItem
{
	private bool _focused;

	public List<TabItem> Items { get; set; }

	public int Index { get; set; }

	public bool IsInList { get; set; }

	public override bool Focused
	{
		get
		{
			return _focused;
		}
		set
		{
			_focused = value;
			if (!value)
			{
				Items[Index].Focused = false;
			}
		}
	}

	public TabSubmenuItem(string name, IEnumerable<TabItem> items)
		: base(name)
	{
		DrawBg = false;
		base.CanBeFocused = true;
		Items = new List<TabItem>(items);
		IsInList = true;
	}

	public void RefreshIndex()
	{
		foreach (TabItem item in Items)
		{
			item.Focused = false;
			item.Active = false;
			item.Visible = false;
		}
		Index = (1000 - 1000 % Items.Count) % Items.Count;
	}

	public override void ProcessControls()
	{
		if (base.JustOpened)
		{
			base.JustOpened = false;
		}
		else
		{
			if (!Focused)
			{
				return;
			}
			if (Game.IsControlJustPressed(0, (Control)176) && Focused && base.Parent.FocusLevel == 1)
			{
				Game.PlaySound("SELECT", "HUD_FRONTEND_DEFAULT_SOUNDSET");
				if (Items[Index].CanBeFocused && !Items[Index].Focused)
				{
					base.Parent.FocusLevel++;
					Items[Index].JustOpened = true;
					Items[Index].Focused = true;
				}
				else
				{
					Items[Index].OnActivated();
				}
			}
			if (Game.IsControlJustPressed(0, (Control)177) && Focused && base.Parent.FocusLevel > 1)
			{
				Game.PlaySound("CANCEL", "HUD_FRONTEND_DEFAULT_SOUNDSET");
				if (Items[Index].CanBeFocused && Items[Index].Focused)
				{
					base.Parent.FocusLevel--;
					Items[Index].Focused = false;
				}
			}
			if ((Game.IsControlJustPressed(0, (Control)188) || Game.IsControlJustPressed(0, (Control)32) || Game.IsControlJustPressed(0, (Control)241)) && base.Parent.FocusLevel == 1)
			{
				Index = (1000 - 1000 % Items.Count + Index - 1) % Items.Count;
				Game.PlaySound("NAV_UP_DOWN", "HUD_FRONTEND_DEFAULT_SOUNDSET");
			}
			else if ((Game.IsControlJustPressed(0, (Control)187) || Game.IsControlJustPressed(0, (Control)33) || Game.IsControlJustPressed(0, (Control)242)) && base.Parent.FocusLevel == 1)
			{
				Index = (1000 - 1000 % Items.Count + Index + 1) % Items.Count;
				Game.PlaySound("NAV_UP_DOWN", "HUD_FRONTEND_DEFAULT_SOUNDSET");
			}
			if (Items.Count > 0)
			{
				Items[Index].ProcessControls();
			}
		}
	}

	public override void Draw()
	{
		if (!Visible)
		{
			return;
		}
		base.Draw();
		_ = Focused;
		int alpha = (Focused ? 200 : 100);
		int alpha2 = (Focused ? 255 : 150);
		float num = Resolution.Width - base.SafeSize.X * 2f;
		int num2 = (int)(num * 0.6818f);
		SizeF sizeF = new SizeF((int)num - (num2 + 3), 40f);
		for (int i = 0; i < Items.Count; i++)
		{
			bool flag = ScreenTools.IsMouseInBounds(base.SafeSize.AddPoints(new PointF(0f, (sizeF.Height + 3f) * (float)i)), sizeF);
			((Rectangle)new UIResRectangle(base.SafeSize.AddPoints(new PointF(0f, (sizeF.Height + 3f) * (float)i)), sizeF, (Index == i && Focused) ? Color.FromArgb(alpha2, Colors.White) : ((flag && Focused) ? Color.FromArgb(100, 50, 50, 50) : Color.FromArgb(alpha, Colors.Black)))).Draw();
			((Text)new UIResText(Items[i].Title, base.SafeSize.AddPoints(new PointF(6f, 5f + (sizeF.Height + 3f) * (float)i)), 0.35f, Color.FromArgb(alpha2, (Index == i && Focused) ? Colors.Black : Colors.White))).Draw();
			if (!(Focused && flag) || !Game.IsControlJustPressed(0, (Control)237))
			{
				continue;
			}
			Items[Index].Focused = false;
			Game.PlaySound("NAV_UP_DOWN", "HUD_FRONTEND_DEFAULT_SOUNDSET");
			bool num3 = Index == i;
			Index = (1000 - 1000 % Items.Count + i) % Items.Count;
			if (num3)
			{
				if (Items[Index].CanBeFocused && !Items[Index].Focused)
				{
					base.Parent.FocusLevel = 2;
					Items[Index].JustOpened = true;
					Items[Index].Focused = true;
				}
				else
				{
					Items[Index].OnActivated();
				}
			}
			else
			{
				base.Parent.FocusLevel = 1;
			}
		}
		Items[Index].Visible = true;
		Items[Index].FadeInWhenFocused = true;
		if (!Items[Index].CanBeFocused)
		{
			Items[Index].Focused = Focused;
		}
		Items[Index].UseDynamicPositionment = false;
		Items[Index].SafeSize = base.SafeSize.AddPoints(new PointF((int)num - num2, 0f));
		Items[Index].TopLeft = base.SafeSize.AddPoints(new PointF((int)num - num2, 0f));
		Items[Index].BottomRight = new PointF((float)(int)Resolution.Width - base.SafeSize.X, (float)(int)Resolution.Height - base.SafeSize.Y);
		if (Items[Index] is TabMissionSelectItem tabMissionSelectItem)
		{
			tabMissionSelectItem._add = -420f;
		}
		Items[Index].Draw();
	}
}
