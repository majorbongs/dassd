using System;
using System.Collections.Generic;
using System.Drawing;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using CitizenFX.Core.UI;

namespace NativeUI.PauseMenu;

public class TabInteractiveListItem : TabItem
{
	protected const int MaxItemsPerView = 15;

	protected int _minItem;

	protected int _maxItem;

	public List<UIMenuItem> Items { get; set; }

	public int Index { get; set; }

	public bool IsInList { get; set; }

	public TabInteractiveListItem(string name, IEnumerable<UIMenuItem> items)
		: base(name)
	{
		DrawBg = false;
		base.CanBeFocused = true;
		Items = new List<UIMenuItem>(items);
		IsInList = true;
		_maxItem = 15;
		_minItem = 0;
	}

	public void MoveDown()
	{
		Index = (1000 - 1000 % Items.Count + Index + 1) % Items.Count;
		if (Items.Count > 15)
		{
			if (Index >= _maxItem)
			{
				_maxItem++;
				_minItem++;
			}
			if (Index == 0)
			{
				_minItem = 0;
				_maxItem = 15;
			}
		}
	}

	public void MoveUp()
	{
		Index = (1000 - 1000 % Items.Count + Index - 1) % Items.Count;
		if (Items.Count > 15)
		{
			if (Index < _minItem)
			{
				_minItem--;
				_maxItem--;
			}
			if (Index == Items.Count - 1)
			{
				_minItem = Items.Count - 15;
				_maxItem = Items.Count;
			}
		}
	}

	public void RefreshIndex()
	{
		Index = 0;
		_maxItem = 15;
		_minItem = 0;
	}

	public override void ProcessControls()
	{
		if (!Visible)
		{
			return;
		}
		if (base.JustOpened)
		{
			base.JustOpened = false;
		}
		else
		{
			if (!Focused || Items.Count == 0)
			{
				return;
			}
			if (Game.IsControlJustPressed(0, (Control)201) && Focused && Items[Index] is UIMenuCheckboxItem)
			{
				Game.PlaySound("SELECT", "HUD_FRONTEND_DEFAULT_SOUNDSET");
				((UIMenuCheckboxItem)Items[Index]).Checked = !((UIMenuCheckboxItem)Items[Index]).Checked;
				((UIMenuCheckboxItem)Items[Index]).CheckboxEventTrigger();
			}
			else if (Game.IsControlJustPressed(0, (Control)201) && Focused)
			{
				Game.PlaySound("SELECT", "HUD_FRONTEND_DEFAULT_SOUNDSET");
				Items[Index].ItemActivate(null);
			}
			if (Game.IsControlJustPressed(0, (Control)189) && Focused)
			{
				Game.PlaySound("NAV_LEFT_RIGHT", "HUD_FRONTEND_DEFAULT_SOUNDSET");
				if (Items[Index] is UIMenuListItem uIMenuListItem)
				{
					uIMenuListItem.Index--;
					uIMenuListItem.ListChangedTrigger(uIMenuListItem.Index);
				}
				else if (Items[Index] is UIMenuSliderItem uIMenuSliderItem)
				{
					uIMenuSliderItem.Value -= uIMenuSliderItem.Multiplier;
					uIMenuSliderItem.SliderChanged(uIMenuSliderItem.Value);
				}
				else if (Items[Index] is UIMenuSliderProgressItem uIMenuSliderProgressItem)
				{
					uIMenuSliderProgressItem.Value--;
					uIMenuSliderProgressItem.SliderProgressChanged(uIMenuSliderProgressItem.Value);
				}
			}
			if (Game.IsControlJustPressed(0, (Control)190) && Focused)
			{
				Game.PlaySound("NAV_LEFT_RIGHT", "HUD_FRONTEND_DEFAULT_SOUNDSET");
				if (Items[Index] is UIMenuListItem uIMenuListItem2)
				{
					uIMenuListItem2.Index++;
					uIMenuListItem2.ListChangedTrigger(uIMenuListItem2.Index);
				}
				else if (Items[Index] is UIMenuSliderItem uIMenuSliderItem2)
				{
					uIMenuSliderItem2.Value += uIMenuSliderItem2.Multiplier;
					uIMenuSliderItem2.SliderChanged(uIMenuSliderItem2.Value);
				}
				else if (Items[Index] is UIMenuSliderProgressItem uIMenuSliderProgressItem2)
				{
					uIMenuSliderProgressItem2.Value++;
					uIMenuSliderProgressItem2.SliderProgressChanged(uIMenuSliderProgressItem2.Value);
				}
			}
			if (Game.IsControlJustPressed(0, (Control)188) || Game.IsControlJustPressed(0, (Control)32) || Game.IsControlJustPressed(0, (Control)241))
			{
				Game.PlaySound("NAV_UP_DOWN", "HUD_FRONTEND_DEFAULT_SOUNDSET");
				MoveUp();
			}
			else if (Game.IsControlJustPressed(0, (Control)187) || Game.IsControlJustPressed(0, (Control)33) || Game.IsControlJustPressed(0, (Control)242))
			{
				Game.PlaySound("NAV_UP_DOWN", "HUD_FRONTEND_DEFAULT_SOUNDSET");
				MoveDown();
			}
		}
	}

	public override void Draw()
	{
		//IL_076f: Unknown result type (might be due to invalid IL or missing references)
		if (!Visible)
		{
			return;
		}
		base.Draw();
		_ = Focused;
		int alpha = (Focused ? 200 : 100);
		int alpha2 = (Focused ? 250 : 150);
		float width = base.BottomRight.X - base.TopLeft.X;
		SizeF sizeF = new SizeF(width, 40f);
		int num = 0;
		for (int i = _minItem; i < Math.Min(Items.Count, _maxItem); i++)
		{
			bool flag = ScreenTools.IsMouseInBounds(base.SafeSize.AddPoints(new PointF(0f, (sizeF.Height + 3f) * (float)num)), sizeF);
			bool flag2 = Items[i].LeftBadge != UIMenuItem.BadgeStyle.None;
			bool flag3 = Items[i].RightBadge != UIMenuItem.BadgeStyle.None;
			bool flag4 = flag3 && flag2;
			bool flag5 = flag3 || flag2;
			((Rectangle)new UIResRectangle(base.SafeSize.AddPoints(new PointF(0f, (sizeF.Height + 3f) * (float)num)), sizeF, (Index == i && Focused) ? Color.FromArgb(alpha2, Colors.White) : ((Focused && flag) ? Color.FromArgb(100, 50, 50, 50) : Color.FromArgb(alpha, Colors.Black)))).Draw();
			((Text)new UIResText(Items[i].Text, base.SafeSize.AddPoints(new PointF(flag4 ? 60 : (flag5 ? 30 : 6), 5f + (sizeF.Height + 3f) * (float)num)), 0.35f, Color.FromArgb(alpha2, (Index == i && Focused) ? Colors.Black : Colors.White))).Draw();
			if (flag2 && !flag3)
			{
				new Sprite(UIMenuItem.BadgeToSpriteLib(Items[i].LeftBadge), UIMenuItem.BadgeToSpriteName(Items[i].LeftBadge, Index == i && Focused), base.SafeSize.AddPoints(new PointF(-2f, 1f + (sizeF.Height + 3f) * (float)num)), new SizeF(40f, 40f), 0f, UIMenuItem.BadgeToColor(Items[i].LeftBadge, Index == i && Focused)).Draw();
			}
			if (!flag2 && flag3)
			{
				new Sprite(UIMenuItem.BadgeToSpriteLib(Items[i].RightBadge), UIMenuItem.BadgeToSpriteName(Items[i].RightBadge, Index == i && Focused), base.SafeSize.AddPoints(new PointF(-2f, 1f + (sizeF.Height + 3f) * (float)num)), new SizeF(40f, 40f), 0f, UIMenuItem.BadgeToColor(Items[i].RightBadge, Index == i && Focused)).Draw();
			}
			if (flag2 && flag3)
			{
				new Sprite(UIMenuItem.BadgeToSpriteLib(Items[i].LeftBadge), UIMenuItem.BadgeToSpriteName(Items[i].LeftBadge, Index == i && Focused), base.SafeSize.AddPoints(new PointF(-2f, 1f + (sizeF.Height + 3f) * (float)num)), new SizeF(40f, 40f), 0f, UIMenuItem.BadgeToColor(Items[i].LeftBadge, Index == i && Focused)).Draw();
				new Sprite(UIMenuItem.BadgeToSpriteLib(Items[i].RightBadge), UIMenuItem.BadgeToSpriteName(Items[i].RightBadge, Index == i && Focused), base.SafeSize.AddPoints(new PointF(25f, 1f + (sizeF.Height + 3f) * (float)num)), new SizeF(40f, 40f), 0f, UIMenuItem.BadgeToColor(Items[i].RightBadge, Index == i && Focused)).Draw();
			}
			if (!string.IsNullOrEmpty(Items[i].RightLabel))
			{
				((Text)new UIResText(Items[i].RightLabel, base.SafeSize.AddPoints(new PointF(base.BottomRight.X - base.SafeSize.X - 5f, 5f + (sizeF.Height + 3f) * (float)num)), 0.35f, Color.FromArgb(alpha2, (Index == i && Focused) ? Colors.Black : Colors.White), (Font)0, (Alignment)2)).Draw();
			}
			if (Items[i] is UIMenuCheckboxItem uIMenuCheckboxItem)
			{
				uIMenuCheckboxItem.Selected = i == Index && Focused;
				uIMenuCheckboxItem._checkedSprite.Position = base.SafeSize.AddPoints(new PointF(base.BottomRight.X - base.SafeSize.X - 60f, -5f + (sizeF.Height + 3f) * (float)num));
				uIMenuCheckboxItem._checkedSprite.TextureName = ((!uIMenuCheckboxItem.Selected) ? ((!uIMenuCheckboxItem.Checked) ? "shop_box_blank" : ((uIMenuCheckboxItem.Style == UIMenuCheckboxStyle.Tick) ? "shop_box_tick" : "shop_box_cross")) : ((!uIMenuCheckboxItem.Checked) ? "shop_box_blankb" : ((uIMenuCheckboxItem.Style == UIMenuCheckboxStyle.Tick) ? "shop_box_tickb" : "shop_box_crossb")));
				uIMenuCheckboxItem._checkedSprite.Draw();
			}
			else if (Items[i] is UIMenuListItem uIMenuListItem)
			{
				int num2 = 5;
				PointF position = base.SafeSize.AddPoints(new PointF(base.BottomRight.X - base.SafeSize.X - 30f, (float)num2 + (sizeF.Height + 3f) * (float)num));
				uIMenuListItem._arrowLeft.Position = position;
				uIMenuListItem._arrowRight.Position = position;
				((Text)uIMenuListItem._itemText).Position = position;
				((Text)uIMenuListItem._itemText).Color = Colors.White;
				((Text)uIMenuListItem._itemText).Font = (Font)0;
				((Text)uIMenuListItem._itemText).Alignment = (Alignment)1;
				uIMenuListItem._itemText.TextAlignment = (Alignment)2;
				string text = uIMenuListItem.Items[uIMenuListItem.Index].ToString();
				float textWidth = ScreenTools.GetTextWidth(text, ((Text)uIMenuListItem._itemText).Font, ((Text)uIMenuListItem._itemText).Scale);
				bool selected = i == Index && Focused;
				uIMenuListItem.Selected = selected;
				((Text)uIMenuListItem._itemText).Color = ((!uIMenuListItem.Enabled) ? Color.FromArgb(163, 159, 148) : (uIMenuListItem.Selected ? Colors.Black : Colors.WhiteSmoke));
				((Text)uIMenuListItem._itemText).Caption = text;
				uIMenuListItem._arrowLeft.Color = ((!uIMenuListItem.Enabled) ? Color.FromArgb(163, 159, 148) : (uIMenuListItem.Selected ? Colors.Black : Colors.WhiteSmoke));
				uIMenuListItem._arrowRight.Color = ((!uIMenuListItem.Enabled) ? Color.FromArgb(163, 159, 148) : (uIMenuListItem.Selected ? Colors.Black : Colors.WhiteSmoke));
				uIMenuListItem._arrowLeft.Position = base.SafeSize.AddPoints(new PointF(base.BottomRight.X - base.SafeSize.X - 60f - (float)(int)textWidth, (float)num2 + (sizeF.Height + 3f) * (float)num));
				if (uIMenuListItem.Selected)
				{
					uIMenuListItem._arrowLeft.Draw();
					uIMenuListItem._arrowRight.Draw();
					((Text)uIMenuListItem._itemText).Position = base.SafeSize.AddPoints(new PointF(base.BottomRight.X - base.SafeSize.X - 30f, (float)num2 + (sizeF.Height + 3f) * (float)num));
				}
				else
				{
					((Text)uIMenuListItem._itemText).Position = base.SafeSize.AddPoints(new PointF(base.BottomRight.X - base.SafeSize.X - 5f, (float)num2 + (sizeF.Height + 3f) * (float)num));
				}
				((Text)uIMenuListItem._itemText).Draw();
			}
			else if (Items[i] is UIMenuSliderItem uIMenuSliderItem)
			{
				int num3 = 15;
				PointF position2 = base.SafeSize.AddPoints(new PointF(base.BottomRight.X - base.SafeSize.X - 210f, (float)num3 + (sizeF.Height + 3f) * (float)num));
				((Rectangle)uIMenuSliderItem._rectangleBackground).Position = position2;
				((Rectangle)uIMenuSliderItem._rectangleBackground).Size = new SizeF(200f, 10f);
				((Rectangle)uIMenuSliderItem._rectangleSlider).Position = position2;
				((Rectangle)uIMenuSliderItem._rectangleSlider).Size = new SizeF(100f, 10f);
				((Rectangle)uIMenuSliderItem._rectangleDivider).Position = base.SafeSize.AddPoints(new PointF(base.BottomRight.X - base.SafeSize.X - 110f, (float)(num3 - 5) + (sizeF.Height + 3f) * (float)num));
				((Rectangle)uIMenuSliderItem._rectangleDivider).Size = new SizeF(2f, 20f);
				if (uIMenuSliderItem.Divider)
				{
					((Rectangle)uIMenuSliderItem._rectangleDivider).Color = Colors.WhiteSmoke;
				}
				uIMenuSliderItem.Selected = i == Index && Focused;
				((Rectangle)uIMenuSliderItem._rectangleSlider).Position = new PointF(position2.X + (float)uIMenuSliderItem._value / (float)uIMenuSliderItem._max * 100f, ((Rectangle)uIMenuSliderItem._rectangleSlider).Position.Y);
				((Rectangle)uIMenuSliderItem._rectangleBackground).Draw();
				((Rectangle)uIMenuSliderItem._rectangleSlider).Draw();
				if (uIMenuSliderItem.Divider)
				{
					((Rectangle)uIMenuSliderItem._rectangleDivider).Draw();
				}
			}
			else if (Items[i] is UIMenuSliderProgressItem uIMenuSliderProgressItem)
			{
				int num4 = 15;
				PointF position3 = base.SafeSize.AddPoints(new PointF(base.BottomRight.X - base.SafeSize.X - 210f, (float)num4 + (sizeF.Height + 3f) * (float)num));
				((Rectangle)uIMenuSliderProgressItem._rectangleBackground).Position = position3;
				((Rectangle)uIMenuSliderProgressItem._rectangleBackground).Size = new SizeF(200f, 10f);
				((Rectangle)uIMenuSliderProgressItem._rectangleSlider).Position = position3;
				((Rectangle)uIMenuSliderProgressItem._rectangleDivider).Position = base.SafeSize.AddPoints(new PointF(base.BottomRight.X - base.SafeSize.X - 100f, (float)(num4 - 5) + (sizeF.Height + 3f) * (float)num));
				((Rectangle)uIMenuSliderProgressItem._rectangleDivider).Size = new SizeF(2f, 20f);
				if (uIMenuSliderProgressItem.Divider)
				{
					((Rectangle)uIMenuSliderProgressItem._rectangleDivider).Color = Colors.WhiteSmoke;
				}
				uIMenuSliderProgressItem.Selected = i == Index && Focused;
				((Rectangle)uIMenuSliderProgressItem._rectangleBackground).Draw();
				((Rectangle)uIMenuSliderProgressItem._rectangleSlider).Draw();
				((Rectangle)uIMenuSliderProgressItem._rectangleDivider).Draw();
				if (ScreenTools.IsMouseInBounds(new PointF(((Rectangle)uIMenuSliderProgressItem._rectangleBackground).Position.X, ((Rectangle)uIMenuSliderProgressItem._rectangleBackground).Position.Y - 5f), new SizeF(200f, ((Rectangle)uIMenuSliderProgressItem._rectangleBackground).Size.Height)))
				{
					if (API.IsDisabledControlPressed(0, 24))
					{
						if (!uIMenuSliderProgressItem.Pressed)
						{
							uIMenuSliderProgressItem.Pressed = true;
							uIMenuSliderProgressItem.Audio.Id = API.GetSoundId();
							API.PlaySoundFrontend(uIMenuSliderProgressItem.Audio.Id, uIMenuSliderProgressItem.Audio.Slider, uIMenuSliderProgressItem.Audio.Library, true);
						}
						float num5 = API.GetDisabledControlNormal(0, 239) * Resolution.Width - ((Rectangle)uIMenuSliderProgressItem._rectangleSlider).Position.X;
						uIMenuSliderProgressItem.Value = (int)Math.Round((float)uIMenuSliderProgressItem._max * ((num5 >= 0f && num5 <= 200f) ? num5 : ((num5 < 0f) ? 0f : 200f)) / 200f);
						uIMenuSliderProgressItem.SliderProgressChanged(uIMenuSliderProgressItem.Value);
					}
					else
					{
						API.StopSound(uIMenuSliderProgressItem.Audio.Id);
						API.ReleaseSoundId(uIMenuSliderProgressItem.Audio.Id);
						uIMenuSliderProgressItem.Pressed = false;
					}
				}
				else
				{
					API.StopSound(uIMenuSliderProgressItem.Audio.Id);
					API.ReleaseSoundId(uIMenuSliderProgressItem.Audio.Id);
					uIMenuSliderProgressItem.Pressed = false;
				}
			}
			if (Focused && flag && Game.IsControlJustPressed(0, (Control)237))
			{
				bool num6 = Index == i;
				Index = (1000 - 1000 % Items.Count + i) % Items.Count;
				if (!num6)
				{
					Game.PlaySound("NAV_UP_DOWN", "HUD_FRONTEND_DEFAULT_SOUNDSET");
				}
				else if (Items[Index] is UIMenuCheckboxItem)
				{
					Game.PlaySound("SELECT", "HUD_FRONTEND_DEFAULT_SOUNDSET");
					((UIMenuCheckboxItem)Items[Index]).Checked = !((UIMenuCheckboxItem)Items[Index]).Checked;
					((UIMenuCheckboxItem)Items[Index]).CheckboxEventTrigger();
				}
				else
				{
					Game.PlaySound("SELECT", "HUD_FRONTEND_DEFAULT_SOUNDSET");
					Items[Index].ItemActivate(null);
				}
			}
			num++;
		}
	}
}
